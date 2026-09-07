using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;

namespace ManagedCode.Orleans.RateLimiting.Tests;

[ClassDataSource<TestClusterApplication>(Shared = SharedType.PerTestSession)]
public class CancellationTests(TestClusterApplication testApp)
{
    private const int PermitCount = 1;
    private const int EmptyQueue = 0;
    private const int QueueLimit = 1;
    private const int Segments = 2;
    private const int TimeoutSeconds = 10;
    private const int PollMilliseconds = 10;
    private const int WindowMinutes = 10;
    private const string Concurrency = "concurrency";
    private const string Fixed = "fixed";
    private const string Sliding = "sliding";
    private const string Token = "token";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(TimeoutSeconds);
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(WindowMinutes);

    [Test]
    [Arguments(Concurrency)]
    [Arguments(Fixed)]
    [Arguments(Sliding)]
    [Arguments(Token)]
    public async Task QueuedAcquisitionCanBeCancelledAcrossOrleans(string algorithm)
    {
        var holder = CreateHolder(algorithm);
        await using var held = await holder.AcquireAndConfigureAsync();
        held.IsAcquired.ShouldBeTrue();
        using var cancellation = new CancellationTokenSource();
        var pending = holder.AcquireAndConfigureAsync(PermitCount, cancellation.Token);
        await WaitForQueueAsync(holder, PermitCount);
        await cancellation.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(() => pending.WaitAsync(Timeout));
        await WaitForQueueAsync(holder, EmptyQueue);
        await held.DisposeAsync();
        if (algorithm == Concurrency)
        {
            await using var next = await holder.AcquireAsync(PermitCount, CancellationToken.None);
            next.IsAcquired.ShouldBeTrue();
        }
    }

    [Test]
    [Arguments(Concurrency)]
    [Arguments(Fixed)]
    [Arguments(Sliding)]
    [Arguments(Token)]
    public async Task PreCancelledAcquisitionDoesNotConsumeQuota(string algorithm)
    {
        var holder = CreateHolder(algorithm);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(() => holder.AcquireAndConfigureAsync(PermitCount, cancellation.Token));
        await using var next = await holder.AcquireAndConfigureAsync();
        next.IsAcquired.ShouldBeTrue();
    }

    [Test]
    public async Task GroupCancellationReleasesEarlierConcurrencyLeases()
    {
        var first = CreateHolder(Concurrency);
        var second = CreateHolder(Concurrency);
        await using var held = await second.AcquireAndConfigureAsync();
        await using var group = new GroupLimiterHolder();
        group.AddLimiter(first);
        group.AddLimiter(second);
        using var cancellation = new CancellationTokenSource();
        var pending = group.AcquireAsync(cancellation.Token);
        await WaitForQueueAsync(second, PermitCount);
        await cancellation.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(() => pending.WaitAsync(Timeout));
        await WaitForQueueAsync(second, EmptyQueue);
        (await first.GetStatisticsAsync())!.CurrentAvailablePermits.ShouldBe(PermitCount);
        await held.DisposeAsync();
        (await group.AcquireAsync()).ShouldBeNull();
    }

    [Test]
    public async Task PreCancelledEmptyGroupDoesNotAllowTheRequest()
    {
        await using var group = new GroupLimiterHolder();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(() => group.AcquireAsync(cancellation.Token));
    }

    private ICancellableLimiterHolder CreateHolder(string algorithm)
    {
        var key = Guid.NewGuid().ToString();
        var client = testApp.Cluster.Client;
        return algorithm switch
        {
            Concurrency => client.GetConcurrencyLimiter(key, new ConcurrencyLimiterOptions
            { PermitLimit = PermitCount, QueueLimit = QueueLimit }),
            Fixed => client.GetFixedWindowRateLimiter(key, new FixedWindowRateLimiterOptions
            { PermitLimit = PermitCount, QueueLimit = QueueLimit, Window = Window, AutoReplenishment = false }),
            Sliding => client.GetSlidingWindowRateLimiter(key, new SlidingWindowRateLimiterOptions
            { PermitLimit = PermitCount, QueueLimit = QueueLimit, Window = Window, SegmentsPerWindow = Segments, AutoReplenishment = false }),
            Token => client.GetTokenBucketRateLimiter(key, new TokenBucketRateLimiterOptions
            { TokenLimit = PermitCount, TokensPerPeriod = PermitCount, QueueLimit = QueueLimit, ReplenishmentPeriod = Window, AutoReplenishment = false }),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
        };
    }

    internal static async Task WaitForQueueAsync(ILimiterHolder holder, long expected)
    {
        using var deadline = new CancellationTokenSource(Timeout);
        while ((await holder.GetStatisticsAsync())!.CurrentQueuedCount != expected)
            await Task.Delay(PollMilliseconds, deadline.Token);
    }
}
