using System.Net;
using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Client.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Exceptions;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;
using ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;
using Microsoft.AspNetCore.Http;
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

        lease.Dispose();
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

        context.Metadata["route"].ShouldBe("/checkout");
        userPartition.ToString().ShouldBe("User:user-policy:user-1");
        groupPartition.ToString().ShouldBe("Group:group-a");
    }
}
