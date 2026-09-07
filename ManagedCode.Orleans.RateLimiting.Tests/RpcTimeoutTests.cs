using ManagedCode.Orleans.RateLimiting.Tests.Cluster.Grains;
using System.Diagnostics;
using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;

namespace ManagedCode.Orleans.RateLimiting.Tests;

[ClassDataSource<TimeoutTestCluster>(Shared = SharedType.PerTestSession)]
public class RpcTimeoutTests(TimeoutTestCluster app)
{
    private const int PermitCount = 1;
    private const int QueueLimit = 16;
    private const int NoQueuedRequests = 0;
    private const int Segments = 2;
    private const int WindowMinutes = 10;
    private const int DeadlineSeconds = 5;
    private const int PollMilliseconds = 10;
    private const string StormReportFormat = "Timeout storm {0}, callerTokenOverload={1}, configure={2}: {3:F1} ms";
    private const string Concurrency = "concurrency";
    private const string Fixed = "fixed";
    private const string Sliding = "sliding";
    private const string Token = "token";
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(DeadlineSeconds);
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(WindowMinutes);

    [Test]
    public async Task SiloToSiloDeadlineAlsoRemovesQueuedAcquisition()
    {
        var key = Guid.NewGuid().ToString();
        var holder = app.Cluster.Client.GetConcurrencyLimiter(key, new ConcurrencyLimiterOptions { PermitLimit = PermitCount, QueueLimit = QueueLimit });
        await using var held = await holder.AcquireAndConfigureAsync();
        var probe = app.Cluster.Client.GetGrain<ITimeoutProbeGrain>(key);
        var pending = probe.AcquireAsync(key);
        await WaitForQueueAsync(holder, PermitCount);
        (await pending.WaitAsync(Deadline)).ShouldBeFalse();
        await WaitForQueueAsync(holder, NoQueuedRequests);
        await held.DisposeAsync();
        (await holder.GetStatisticsAsync())!.CurrentAvailablePermits.ShouldBe(PermitCount);
        await holder.DeleteStateAsync();
    }

    [Test]
    public async Task RpcTimeoutRemovesQueuedAcquisition()
    {
        var holder = CreateHolder(Concurrency);
        await using var held = await holder.AcquireAndConfigureAsync();
        using var cancellation = new CancellationTokenSource();
        try
        {
            var pending = holder.AcquireAsync(PermitCount, cancellation.Token);
            await WaitForQueueAsync(holder, PermitCount);
            await using var rejected = await pending.WaitAsync(Deadline);
            rejected.IsAcquired.ShouldBeFalse();
            await WaitForQueueAsync(holder, NoQueuedRequests);
        }
        finally
        {
            await cancellation.CancelAsync();
        }
    }

    [Test]
    [Arguments(Concurrency, false, false)]
    [Arguments(Concurrency, true, false)]
    [Arguments(Concurrency, false, true)]
    [Arguments(Concurrency, true, true)]
    [Arguments(Fixed, false, false)]
    [Arguments(Fixed, true, false)]
    [Arguments(Fixed, false, true)]
    [Arguments(Fixed, true, true)]
    [Arguments(Sliding, false, false)]
    [Arguments(Sliding, true, false)]
    [Arguments(Sliding, false, true)]
    [Arguments(Sliding, true, true)]
    [Arguments(Token, false, false)]
    [Arguments(Token, true, false)]
    [Arguments(Token, false, true)]
    [Arguments(Token, true, true)]
    public async Task TimeoutStormLeavesNoQueuedOrOrphanedPermits(string algorithm, bool cancellable, bool configure)
    {
        var holder = CreateHolder(algorithm);
        await using var held = await holder.AcquireAndConfigureAsync();
        using var callerCancellation = new CancellationTokenSource();
        var started = Stopwatch.GetTimestamp();
        var pending = Enumerable.Range(NoQueuedRequests, QueueLimit).Select(_ => AcquireAsync(holder, cancellable, configure, callerCancellation.Token)).ToArray();
        await WaitForQueueAsync(holder, QueueLimit);
        var results = await Task.WhenAll(pending).WaitAsync(Deadline);
        foreach (var lease in results)
        {
            lease.IsAcquired.ShouldBeFalse();
            await lease.DisposeAsync();
        }
        await WaitForQueueAsync(holder, NoQueuedRequests);
        var elapsed = Stopwatch.GetElapsedTime(started);
        elapsed.ShouldBeLessThan(Deadline);
        Console.WriteLine(StormReportFormat, algorithm, cancellable, configure, elapsed.TotalMilliseconds);
        await held.DisposeAsync();
        if (algorithm == Concurrency)
        {
            (await holder.GetStatisticsAsync())!.CurrentAvailablePermits.ShouldBe(PermitCount);
            await using var next = await holder.AcquireAndConfigureAsync();
            next.IsAcquired.ShouldBeTrue();
        }
        await holder.DeleteStateAsync();
    }

    private static Task<OrleansRateLimitLease> AcquireAsync(ICancellableLimiterHolder holder, bool cancellable, bool configure, CancellationToken cancellationToken)
        => (cancellable, configure) switch
        {
            (true, true) => holder.AcquireAndConfigureAsync(PermitCount, cancellationToken),
            (true, false) => holder.AcquireAsync(PermitCount, cancellationToken),
            (false, true) => AcquireLegacyAsync(holder, configure),
            _ => AcquireLegacyAsync(holder, configure)
        };

    private static Task<OrleansRateLimitLease> AcquireLegacyAsync(ILimiterHolder holder, bool configure)
        => configure ? holder.AcquireAndConfigureAsync() : holder.AcquireAsync();

    private ICancellableLimiterHolder CreateHolder(string algorithm)
    {
        var key = Guid.NewGuid().ToString();
        var client = app.Cluster.Client;
        return algorithm switch
        {
            Concurrency => client.GetConcurrencyLimiter(key, new ConcurrencyLimiterOptions { PermitLimit = PermitCount, QueueLimit = QueueLimit }),
            Fixed => client.GetFixedWindowRateLimiter(key, new FixedWindowRateLimiterOptions
            { PermitLimit = PermitCount, QueueLimit = QueueLimit, Window = Window, AutoReplenishment = false }),
            Sliding => client.GetSlidingWindowRateLimiter(key, new SlidingWindowRateLimiterOptions
            { PermitLimit = PermitCount, QueueLimit = QueueLimit, Window = Window, SegmentsPerWindow = Segments, AutoReplenishment = false }),
            Token => client.GetTokenBucketRateLimiter(key, new TokenBucketRateLimiterOptions
            { TokenLimit = PermitCount, QueueLimit = QueueLimit, TokensPerPeriod = PermitCount, ReplenishmentPeriod = Window, AutoReplenishment = false }),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
        };
    }

    private static async Task WaitForQueueAsync(ILimiterHolder holder, long expected)
    {
        using var deadline = new CancellationTokenSource(Deadline);
        while ((await holder.GetStatisticsAsync())!.CurrentQueuedCount != expected)
            await Task.Delay(PollMilliseconds, deadline.Token);
    }
}
