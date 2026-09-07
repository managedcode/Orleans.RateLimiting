using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Server.Grains;
using ManagedCode.Orleans.RateLimiting.Server.Options;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;
using Microsoft.Extensions.Logging.Abstractions;
using OptionsFactory = Microsoft.Extensions.Options.Options;
using Orleans.Runtime;

namespace ManagedCode.Orleans.RateLimiting.Tests;

public class PersistenceFailureTests
{
    private const int TestPermitLimit = 1;
    private const int NoPermits = 0;
    private const int ExpectedWrites = 2;
    private const int TimeoutSeconds = 10;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(TimeoutSeconds);

    [Test]
    public async Task FailedClearPreservesActiveRuntimeQuotaAndState()
    {
        var state = new FaultingPersistentState<ConcurrencyLimiterOptions> { FailClears = true };
        using var grain = new TestConcurrencyGrain(state);
        using var lease = grain.TakePermit();
        var originalState = state.State;
        await Should.ThrowAsync<IOException>(() => grain.DeleteStateAsync().AsTask());
        state.State.ShouldBeSameAs(originalState);
        (await grain.GetStatisticsAsync())!.CurrentAvailablePermits.ShouldBe(NoPermits);
        lease.Dispose();
        (await grain.GetStatisticsAsync())!.CurrentAvailablePermits.ShouldBe(TestPermitLimit);
    }

    [Test]
    public async Task FailedWriteIsRetriedDuringFinalFlush()
    {
        var state = new FaultingPersistentState<ConcurrencyLimiterOptions> { FailWrites = true };
        using var grain = new TestConcurrencyGrain(state);
        await Should.ThrowAsync<IOException>(() => grain.ResetAsync().AsTask());
        state.FailWrites = false;
        await grain.OnDeactivateAsync(default, CancellationToken.None);
        state.Writes.ShouldBe(ExpectedWrites);
        state.State.HasSnapshot.ShouldBeTrue();
        state.State.CurrentAvailablePermits.ShouldBe(TestPermitLimit);
    }

    [Test]
    public async Task DeactivationCancellationReachesPendingStorageWrite()
    {
        var state = new FaultingPersistentState<ConcurrencyLimiterOptions> { BlockWrites = true };
        using var grain = new TestConcurrencyGrain(state);
        using var cancellation = new CancellationTokenSource();
        var pending = grain.OnDeactivateAsync(default, cancellation.Token);
        await state.WriteStarted.Task.WaitAsync(Timeout);
        state.WriteToken.ShouldBe(cancellation.Token);
        await cancellation.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(() => pending.WaitAsync(Timeout));
        state.State.HasSnapshot.ShouldBeTrue();
    }

    private sealed class TestConcurrencyGrain(FaultingPersistentState<ConcurrencyLimiterOptions> state)
        : ConcurrencyLimiterGrain(NullLogger<ConcurrencyLimiterGrain>.Instance,
            OptionsFactory.Create(new ConcurrencyLimiterOptions { PermitLimit = TestPermitLimit }),
            OptionsFactory.Create(new RateLimiterPersistenceOptions { StateFlushPeriod = TimeSpan.Zero }), state)
    {
        public RateLimitLease TakePermit() => RateLimiter.AttemptAcquire(TestPermitLimit);
    }
}
