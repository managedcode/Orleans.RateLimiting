using System.Net;
using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Client.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Exceptions;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;
using ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;
using ManagedCode.Orleans.RateLimiting.Core.Options;
using ManagedCode.Orleans.RateLimiting.Core.Services;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Orleans.Runtime;

namespace ManagedCode.Orleans.RateLimiting.Tests;

[ClassDataSource<TestClusterApplication>(Shared = SharedType.PerTestSession)]
public class CoreCoverageTests
{
    private readonly TestClusterApplication _testApp;

    public CoreCoverageTests(TestClusterApplication testApp)
    {
        _testApp = testApp;
    }

    [Test]
    public void GenericFactoryRoutesKnownLimiterInterfaces()
    {
        var client = _testApp.Cluster.Client;

        client.GetRateLimiter<IFixedWindowRateLimiterGrain>("generic-fixed").ShouldBeOfType<FixedWindowRateLimiterHolder>();
        client.GetRateLimiter<IConcurrencyLimiterGrain>("generic-concurrency").ShouldBeOfType<ConcurrencyLimiterHolder>();
        client.GetRateLimiter<ISlidingWindowRateLimiterGrain>("generic-sliding").ShouldBeOfType<SlidingWindowRateLimiterHolder>();
        client.GetRateLimiter<ITokenBucketRateLimiterGrain>("generic-token").ShouldBeOfType<TokenBucketRateLimiterHolder>();
    }

    [Test]
    public void ConfigFactoryRoutesOptionsAndIgnoresMissingConfig()
    {
        var client = _testApp.Cluster.Client;
        var configs = new RateLimiterConfig[]
        {
            new("fixed", new FixedWindowRateLimiterOptions { PermitLimit = 1, QueueLimit = 0, Window = TimeSpan.FromSeconds(1) }),
            new("concurrency", new ConcurrencyLimiterOptions { PermitLimit = 1, QueueLimit = 0 }),
            new("sliding", new SlidingWindowRateLimiterOptions { PermitLimit = 1, QueueLimit = 0, SegmentsPerWindow = 2, Window = TimeSpan.FromSeconds(1) }),
            new("token", new TokenBucketRateLimiterOptions { TokenLimit = 1, TokensPerPeriod = 1, QueueLimit = 0, ReplenishmentPeriod = TimeSpan.FromSeconds(1) })
        };

        client.GetRateLimiterByConfig("config-fixed", "fixed", configs).ShouldBeOfType<FixedWindowRateLimiterHolder>();
        client.GetRateLimiterByConfig("config-concurrency", "CONCURRENCY", configs).ShouldBeOfType<ConcurrencyLimiterHolder>();
        client.GetRateLimiterByConfig("config-sliding", "sliding", configs).ShouldBeOfType<SlidingWindowRateLimiterHolder>();
        client.GetRateLimiterByConfig("config-token", "token", configs).ShouldBeOfType<TokenBucketRateLimiterHolder>();
        client.GetRateLimiterByConfig("config-missing", "missing", configs).ShouldBeNull();
    }

    [Test]
    public async Task OptionBackedHoldersApplyConfigurationDuringAcquire()
    {
        var client = _testApp.Cluster.Client;

        var concurrency = client.GetConcurrencyLimiter("holder-concurrency", new ConcurrencyLimiterOptions { PermitLimit = 2, QueueLimit = 0 });
        await using (var lease = await concurrency.AcquireAndCheckConfigurationAsync(1, default!))
            lease.IsAcquired.ShouldBeTrue();
        (await concurrency.GetConfiguration()).PermitLimit.ShouldBe(2);

        var fixedWindow = client.GetFixedWindowRateLimiter("holder-fixed", new FixedWindowRateLimiterOptions { PermitLimit = 2, QueueLimit = 0, Window = TimeSpan.FromSeconds(1) });
        await using (var lease = await fixedWindow.AcquireAndConfigureAsync())
            lease.IsAcquired.ShouldBeTrue();
        (await fixedWindow.GetConfiguration()).PermitLimit.ShouldBe(2);

        var slidingWindow = client.GetSlidingWindowRateLimiter("holder-sliding", new SlidingWindowRateLimiterOptions { PermitLimit = 2, QueueLimit = 0, SegmentsPerWindow = 2, Window = TimeSpan.FromSeconds(1) });
        await using (var lease = await slidingWindow.AcquireAndCheckConfigurationAsync(default!))
            lease.IsAcquired.ShouldBeTrue();
        (await slidingWindow.GetConfiguration()).SegmentsPerWindow.ShouldBe(2);

        var tokenBucket = client.GetTokenBucketRateLimiter("holder-token", new TokenBucketRateLimiterOptions { TokenLimit = 2, TokensPerPeriod = 2, QueueLimit = 0, ReplenishmentPeriod = TimeSpan.FromSeconds(1) });
        await using (var lease = await tokenBucket.AcquireAndCheckConfigurationAsync(1, default!))
            lease.IsAcquired.ShouldBeTrue();
        (await tokenBucket.GetConfiguration()).TokenLimit.ShouldBe(2);
    }

    [Test]
    public async Task LeaseMetadataAndExceptionsExposeRejectedState()
    {
        var metadata = new RateLimitLeaseMetadata(GrainId.Create("coverage", "lease"));
        var lease = new OrleansRateLimitLease(metadata, _testApp.Cluster.Client);

        lease.IsAcquired.ShouldBeFalse();
        lease.Reason.ShouldBe("Lease not acquired");
        lease.RetryAfter.ShouldBe(TimeSpan.Zero);
        lease.MetadataNames.ShouldContain("REASON_PHRASE");
        lease.GetAllMetadata().ShouldContain(pair => pair.Key == "REASON_PHRASE");

        var exception = lease.ToException();
        exception.Reason.ShouldBe("Lease not acquired");
        Should.Throw<RateLimitExceededException>(() => lease.ThrowIfNotAcquired()).Reason.ShouldBe("Lease not acquired");

        new RateLimitExceededException().RetryAfter.ShouldBe(TimeSpan.Zero);
        new RateLimitExceededException("limited").Reason.ShouldBe("limited");
        new RateLimitExceededException(TimeSpan.FromSeconds(3)).RetryAfter.ShouldBe(TimeSpan.FromSeconds(3));

        await lease.DisposeAsync();
    }

    [Test]
    public void RequestIpUsesFirstForwardedHeaderValueAndRemoteFallback()
    {
        var forwarded = new DefaultHttpContext();
        forwarded.Request.Headers["X-Forwarded-For"] = "203.0.113.10, 203.0.113.11";

        forwarded.Request.GetClientIpAddress(["X-Forwarded-For"]).ShouldBe("203.0.113.10");

        var remote = new DefaultHttpContext();
        remote.Connection.RemoteIpAddress = IPAddress.Parse("198.51.100.5");

        remote.Request.GetClientIpAddress(["X-Missing"]).ShouldBe("198.51.100.5");
    }

    [Test]
    public void RequestOrchestrationModelsDescribeUserAndGroupPartitions()
    {
        var context = new RateLimitRequestContext
        {
            OperationName = "checkout",
            UserId = "user-1",
            GroupId = "group-a",
            TenantId = "tenant-main",
            Metadata = new Dictionary<string, string>
            {
                ["route"] = "/checkout"
            }
        };

        var userPartition = new RateLimitRequestPartition(RateLimitPartitionKind.User, context.UserId!, "user-policy");
        var groupPartition = new RateLimitRequestPartition(RateLimitPartitionKind.Group, context.GroupId!);
        var userRule = new RateLimitRequestRule(RateLimitPartitionKind.User, "user-policy") { Required = true };
        var signalRRule = new RateLimitRequestRule(RateLimitPartitionKind.User, "hub-policy") { PolicyName = "SignalR" };

        context.Metadata["route"].ShouldBe("/checkout");
        userPartition.ToString().ShouldBe("User:user-policy:user-1");
        groupPartition.ToString().ShouldBe("Group:group-a");
        userRule.ConfigurationName.ShouldBe("user-policy");
        userRule.Required.ShouldBeTrue();
        signalRRule.PolicyName.ShouldBe("SignalR");
    }

    [Test]
    public async Task DefaultRequestOrchestratorCreatesLimitersForConfiguredPartitions()
    {
        var options = new RateLimitRequestOrchestrationOptions()
            .AddUser("fixed", required: true)
            .AddGroup("fixed")
            .AddCustom("fixed", "route", keyPrefix: "custom");

        var configs = new RateLimiterConfig[]
        {
            new("fixed", new FixedWindowRateLimiterOptions { PermitLimit = 10, QueueLimit = 0, Window = TimeSpan.FromSeconds(1) })
        };

        var orchestrator = new DefaultRateLimitRequestOrchestrator(
            _testApp.Cluster.Client,
            configs,
            [new OptionsRateLimitRequestPolicy(Options.Create(options))],
            new DefaultRateLimitRequestKeyResolver());

        await using var holder = await orchestrator.CreateLimiterGroupAsync(new RateLimitRequestContext
        {
            OperationName = "checkout",
            UserId = "user-1",
            GroupId = "group-a",
            Metadata = new Dictionary<string, string> { ["route"] = "/checkout" }
        });

        holder.Count.ShouldBe(3);
        var lease = await holder.AcquireAsync();
        lease.ShouldBeNull();
        await Should.ThrowAsync<InvalidOperationException>(async () => await holder.AcquireAsync());
    }

    [Test]
    public async Task DefaultRequestOrchestratorFailsFastForRequiredMissingKeysAndConfigs()
    {
        var requiredUser = new RateLimitRequestOrchestrationOptions().AddUser("fixed", required: true);
        var missingConfig = new RateLimitRequestOrchestrationOptions().AddUser("missing", required: true);

        var configs = new RateLimiterConfig[]
        {
            new("fixed", new FixedWindowRateLimiterOptions { PermitLimit = 1, QueueLimit = 0, Window = TimeSpan.FromSeconds(1) })
        };

        var orchestrator = new DefaultRateLimitRequestOrchestrator(
            _testApp.Cluster.Client,
            configs,
            [new OptionsRateLimitRequestPolicy(Options.Create(requiredUser))],
            new DefaultRateLimitRequestKeyResolver());

        await Should.ThrowAsync<RateLimitPartitionKeyNotFoundException>(async () =>
            await orchestrator.CreateLimiterGroupAsync(new RateLimitRequestContext { OperationName = "checkout" }));

        orchestrator = new DefaultRateLimitRequestOrchestrator(
            _testApp.Cluster.Client,
            configs,
            [new OptionsRateLimitRequestPolicy(Options.Create(missingConfig))],
            new DefaultRateLimitRequestKeyResolver());

        await Should.ThrowAsync<RateLimitConfigurationNotFoundException>(async () =>
            await orchestrator.CreateLimiterGroupAsync(new RateLimitRequestContext { OperationName = "checkout", UserId = "user-1" }));
    }

    [Test]
    public async Task RequestOrchestratorSeparatesNamedPolicies()
    {
        var options = new RateLimitRequestOrchestrationOptions()
            .AddUser("http", required: true)
            .AddToPolicy("SignalR", RateLimitPartitionKind.User, "signalr", required: true);

        var configs = new RateLimiterConfig[]
        {
            new("http", new FixedWindowRateLimiterOptions { PermitLimit = 10, QueueLimit = 0, Window = TimeSpan.FromSeconds(1) }),
            new("signalr", new FixedWindowRateLimiterOptions { PermitLimit = 10, QueueLimit = 0, Window = TimeSpan.FromSeconds(1) })
        };

        var orchestrator = new DefaultRateLimitRequestOrchestrator(
            _testApp.Cluster.Client,
            configs,
            [new OptionsRateLimitRequestPolicy(Options.Create(options))],
            new DefaultRateLimitRequestKeyResolver());

        await using var httpHolder = await orchestrator.CreateLimiterGroupAsync(new RateLimitRequestContext
        {
            OperationName = "checkout",
            UserId = "user-1"
        });

        await using var signalRHolder = await orchestrator.CreateLimiterGroupAsync(new RateLimitRequestContext
        {
            OperationName = "DoTest",
            UserId = "user-1",
            PolicyName = "SignalR"
        });

        httpHolder.Count.ShouldBe(1);
        signalRHolder.Count.ShouldBe(1);
    }
}
