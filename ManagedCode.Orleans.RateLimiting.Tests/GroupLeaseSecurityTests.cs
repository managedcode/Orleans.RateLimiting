using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;

namespace ManagedCode.Orleans.RateLimiting.Tests;

[ClassDataSource<TestClusterApplication>(Shared = SharedType.PerTestSession)]
public class GroupLeaseSecurityTests(TestClusterApplication testApp)
{
    private const int PermitLimit = 2;
    private const int InvalidPermitLimit = 0;
    private const int QueueLimit = 0;

    [Test]
    public async Task ExceptionDuringGroupAcquisitionImmediatelyReturnsEarlierConcurrencyPermits()
    {
        var limiter = CreateLimiter();
        var invalid = testApp.Cluster.Client.GetConcurrencyLimiter(Guid.NewGuid().ToString(),
            new ConcurrencyLimiterOptions { PermitLimit = InvalidPermitLimit, QueueLimit = QueueLimit });
        await using var group = new GroupLimiterHolder();
        group.AddLimiter(limiter);
        group.AddLimiter(invalid);

        await Should.ThrowAsync<ArgumentException>(() => group.AcquireAsync());
        (await limiter.GetStatisticsAsync())!.CurrentAvailablePermits.ShouldBe(PermitLimit);
    }

    [Test]
    public async Task OverlappingAcquisitionsCannotOverwriteAndLeakLeases()
    {
        var limiter = CreateLimiter();
        var gated = new GatedHolder(limiter);
        await using var group = new GroupLimiterHolder();
        group.AddLimiter(gated);
        var first = group.AcquireAsync();
        await gated.Acquired.Task;
        var second = group.AcquireAsync();
        try
        {
            second.IsCompleted.ShouldBeTrue();
            await Should.ThrowAsync<InvalidOperationException>(() => second);
        }
        finally
        {
            gated.Continue.TrySetResult();
            await ObserveAsync(first);
            await ObserveAsync(second);
            await group.DisposeAsync();
        }
        (await limiter.GetStatisticsAsync())!.CurrentAvailablePermits.ShouldBe(PermitLimit);
    }

    [Test]
    public async Task DisposeWaitsForInflightAcquisitionAndReturnsItsPermit()
    {
        var limiter = CreateLimiter();
        var gated = new GatedHolder(limiter);
        await using var group = new GroupLimiterHolder();
        group.AddLimiter(gated);
        var acquisition = group.AcquireAsync();
        await gated.Acquired.Task;
        var disposal = group.DisposeAsync().AsTask();
        try
        {
            disposal.IsCompleted.ShouldBeFalse();
        }
        finally
        {
            gated.Continue.TrySetResult();
            await acquisition;
            await disposal;
        }
        (await limiter.GetStatisticsAsync())!.CurrentAvailablePermits.ShouldBe(PermitLimit);
    }

    [Test]
    public async Task GroupCannotBeMutatedDuringAcquisition()
    {
        var gated = new GatedHolder(CreateLimiter());
        await using var group = new GroupLimiterHolder();
        group.AddLimiter(gated);
        var acquisition = group.AcquireAsync();
        await gated.Acquired.Task;
        try
        {
            Should.Throw<InvalidOperationException>(() => group.AddLimiter(CreateLimiter()));
        }
        finally
        {
            gated.Continue.TrySetResult();
            await acquisition;
        }
    }

    [Test]
    public async Task EveryDisposeCallerWaitsUntilThePendingLeaseIsReleased()
    {
        var limiter = CreateLimiter();
        var gated = new GatedHolder(limiter);
        await using var group = new GroupLimiterHolder();
        group.AddLimiter(gated);
        var acquisition = group.AcquireAsync();
        await gated.Acquired.Task;
        var first = group.DisposeAsync().AsTask();
        var second = group.DisposeAsync().AsTask();
        try
        {
            second.IsCompleted.ShouldBeFalse();
        }
        finally
        {
            gated.Continue.TrySetResult();
            await acquisition;
            await Task.WhenAll(first, second);
        }
        (await limiter.GetStatisticsAsync())!.CurrentAvailablePermits.ShouldBe(PermitLimit);
    }

    [Test]
    public async Task RejectionRollsBackConcurrencyAndAllowsRetryAfterReset()
    {
        var limiter = CreateLimiter();
        var quota = testApp.Cluster.Client.GetFixedWindowRateLimiter(Guid.NewGuid().ToString(), new FixedWindowRateLimiterOptions
        {
            PermitLimit = SecurityGrainOptions.StrictPermits,
            QueueLimit = QueueLimit,
            Window = TimeSpan.FromSeconds(SecurityGrainOptions.WindowSeconds)
        });
        await using var consumed = await quota.AcquireAndConfigureAsync();
        consumed.IsAcquired.ShouldBeTrue();
        await using var group = new GroupLimiterHolder();
        group.AddLimiter(limiter);
        group.AddLimiter(quota);

        (await group.AcquireAsync()).ShouldNotBeNull();
        (await limiter.GetStatisticsAsync())!.CurrentAvailablePermits.ShouldBe(PermitLimit);
        await quota.ResetAsync();
        (await group.AcquireAsync()).ShouldBeNull();
        await group.DisposeAsync();
        (await limiter.GetStatisticsAsync())!.CurrentAvailablePermits.ShouldBe(PermitLimit);
    }

    private ConcurrencyLimiterHolder CreateLimiter() => testApp.Cluster.Client.GetConcurrencyLimiter(Guid.NewGuid().ToString(),
        new ConcurrencyLimiterOptions { PermitLimit = PermitLimit, QueueLimit = QueueLimit });

    private static async Task ObserveAsync(Task task)
    {
        try { await task; }
        catch (InvalidOperationException) { }
    }

    // Delays delivery of a real Orleans lease to deterministically expose lifecycle races.
    private sealed class GatedHolder(ILimiterHolder inner) : ILimiterHolder
    {
        public TaskCompletionSource Acquired { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Continue { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<OrleansRateLimitLease> AcquireAsync(int permitCount = SecurityGrainOptions.StrictPermits) => inner.AcquireAsync(permitCount);
        public async Task<OrleansRateLimitLease> AcquireAndConfigureAsync(int permitCount = SecurityGrainOptions.StrictPermits)
        {
            var lease = await inner.AcquireAndConfigureAsync(permitCount);
            Acquired.TrySetResult();
            await Continue.Task;
            return lease;
        }
        public ValueTask<RateLimiterStatistics?> GetStatisticsAsync() => inner.GetStatisticsAsync();
        public ValueTask ResetAsync() => inner.ResetAsync();
        public ValueTask DeleteStateAsync() => inner.DeleteStateAsync();
    }
}
