using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;

namespace ManagedCode.Orleans.RateLimiting.Tests;

[ClassDataSource<TimeoutTestCluster>(Shared = SharedType.PerTestSession)]
public class DeadlineQueueTests(TimeoutTestCluster app)
{
    private const int PermitCount = 1;
    private const int ChangedLimit = 2;
    private const int QueueLimit = 16;
    private const int PollMilliseconds = 10;
    private const int TestDeadlineSeconds = 5;
    private static readonly TimeSpan TestDeadline = TimeSpan.FromSeconds(TestDeadlineSeconds);

    [Test]
    public async Task ExpiredConfigurationWaitDoesNotReplaceQuotaOrLeaveQueueWaiters()
    {
        var key = Guid.NewGuid().ToString();
        var holder = app.Cluster.Client.GetConcurrencyLimiter(key, Options(PermitCount));
        await using var held = await holder.AcquireAndConfigureAsync();
        using var cancellation = new CancellationTokenSource();
        var grain = app.Cluster.Client.GetGrain<IConcurrencyLimiterGrain>(key).AsReference<ICancellableRateLimiterGrain>();
        var waiting = grain.AcquireAsync(PermitCount, cancellation.Token);
        try
        {
            await WaitForQueueAsync(holder, PermitCount);
            var replacement = app.Cluster.Client.GetConcurrencyLimiter(key, Options(ChangedLimit));
            await using var rejected = await replacement.AcquireAndConfigureAsync().WaitAsync(TestDeadline);
            rejected.IsAcquired.ShouldBeFalse();
            (await holder.GetConfiguration()).PermitLimit.ShouldBe(PermitCount);
        }
        finally { await cancellation.CancelAsync(); }
        await Should.ThrowAsync<OperationCanceledException>(() => waiting);
        await WaitForQueueAsync(holder, default);
        await held.DisposeAsync();
        (await holder.GetStatisticsAsync())!.CurrentAvailablePermits.ShouldBe(PermitCount);
        await holder.DeleteStateAsync();
    }

    [Test]
    public async Task QueuedSuccessPreservesCountersAndReturnsItsPermit()
    {
        var holder = app.Cluster.Client.GetConcurrencyLimiter(Guid.NewGuid().ToString(), Options(PermitCount));
        await using var held = await holder.AcquireAndConfigureAsync();
        var waiting = holder.AcquireAsync();
        await WaitForQueueAsync(holder, PermitCount);
        await held.DisposeAsync();
        await using var granted = await waiting.WaitAsync(TestDeadline);
        granted.IsAcquired.ShouldBeTrue();
        var statistics = (await holder.GetStatisticsAsync())!;
        statistics.TotalSuccessfulLeases.ShouldBe(ChangedLimit);
        statistics.TotalFailedLeases.ShouldBe(default);
        await granted.DisposeAsync();
        (await holder.GetStatisticsAsync())!.CurrentAvailablePermits.ShouldBe(PermitCount);
        await holder.DeleteStateAsync();
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task ExpiredExplicitBudgetCannotAcquireOrReconfigure(bool configure)
    {
        var key = Guid.NewGuid().ToString();
        var holder = app.Cluster.Client.GetConcurrencyLimiter(key, Options(PermitCount));
        await holder.Configure(Options(PermitCount));
        var grain = app.Cluster.Client.GetGrain<IConcurrencyLimiterGrain>(key).AsReference<IBoundedRateLimiterGrain<ConcurrencyLimiterOptions>>();
        if (configure)
            await Should.ThrowAsync<TimeoutException>(() => grain.AcquireAndCheckConfigurationWithDeadlineAsync(PermitCount, Options(ChangedLimit), TimeSpan.Zero));
        else
            await Should.ThrowAsync<TimeoutException>(() => grain.AcquireWithDeadlineAsync(PermitCount, TimeSpan.Zero));
        (await holder.GetConfiguration()).PermitLimit.ShouldBe(PermitCount);
        (await holder.GetStatisticsAsync())!.CurrentAvailablePermits.ShouldBe(PermitCount);
        await holder.DeleteStateAsync();
    }

    private static ConcurrencyLimiterOptions Options(int permits) => new() { PermitLimit = permits, QueueLimit = QueueLimit };

    private static async Task WaitForQueueAsync(ILimiterHolder holder, long count)
    {
        using var timeout = new CancellationTokenSource(TestDeadline);
        while ((await holder.GetStatisticsAsync())!.CurrentQueuedCount != count)
            await Task.Delay(PollMilliseconds, timeout.Token);
    }
}
