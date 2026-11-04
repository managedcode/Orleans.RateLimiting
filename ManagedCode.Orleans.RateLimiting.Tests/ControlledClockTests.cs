using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Orleans.TestingHost;
using Orleans.Storage;
using Orleans.Runtime;
using ManagedCode.Orleans.RateLimiting.Server;

namespace ManagedCode.Orleans.RateLimiting.Tests;

public class ControlledClockTests
{
    private const string FailedOutcome = "failed";
    private const string DeadlineMessage = "The rate limiter acquisition deadline expired.";
    private const short SiloCount = 1;
    private const int PermitCount = 1;
    private const int AdvanceMilliseconds = 100;
    private const int PollMilliseconds = 10;
    private const int TimeoutSeconds = 10;
    private const int AcquisitionBudgetMinutes = 1;
    private static readonly TimeSpan Advance = TimeSpan.FromMilliseconds(AdvanceMilliseconds);
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(TimeoutSeconds);
    private static readonly TimeSpan AcquisitionBudget = TimeSpan.FromMinutes(AcquisitionBudgetMinutes);

    [Test]
    public async Task SnapshotTimestampsFollowTheInjectedClock()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var storage = new StorageFaultControl();
        await using var cluster = CreateClusterBuilder(clock, storage).Build();
        await cluster.DeployAsync();
        var holder = cluster.Client.GetConcurrencyLimiter(Guid.NewGuid().ToString());

        await holder.ResetAsync();
        storage.SnapshotTimes.Last().ShouldBe(clock.GetUtcNow());
        clock.Advance(Advance);
        await holder.ResetAsync();
        storage.SnapshotTimes.Last().ShouldBe(clock.GetUtcNow());
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task AdvancingTheGrainClockExpiresQueuedAcquisitions(bool cancellable)
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        await using var cluster = CreateClusterBuilder(clock).Build();
        await cluster.DeployAsync();
        var key = Guid.NewGuid().ToString();
        var holder = cluster.Client.GetConcurrencyLimiter(key,
            new ConcurrencyLimiterOptions { PermitLimit = PermitCount, QueueLimit = PermitCount });
        await using var held = await holder.AcquireAndConfigureAsync();
        using var capture = new MetricCapture(MetricCapture.AcquisitionDuration);
        var grain = cluster.Client.GetGrain<IConcurrencyLimiterGrain>(key).AsReference<IBoundedRateLimiterGrain>();
        using var caller = new CancellationTokenSource();
        var waiting = cancellable
            ? grain.AcquireCancellableWithDeadlineAsync(PermitCount, AcquisitionBudget, caller.Token)
            : grain.AcquireWithDeadlineAsync(PermitCount, AcquisitionBudget);

        await CancellationTests.WaitForQueueAsync(holder, PermitCount);
        clock.Advance(AcquisitionBudget);

        var exception = await Should.ThrowAsync<TimeoutException>(() => waiting.WaitAsync(TestTimeout));
        exception.Message.ShouldBe(DeadlineMessage);
        capture.Measurements.ShouldContain(item => item.Value == AcquisitionBudget.TotalSeconds);
        (await holder.GetStatisticsAsync())!.CurrentQueuedCount.ShouldBe(default(long));
        await held.DisposeAsync();
        (await holder.GetStatisticsAsync())!.CurrentAvailablePermits.ShouldBe(PermitCount);
    }

    [Test]
    public async Task AdvancingTheGrainClockExpiresAcquisitionWaitingForTheConfigurationLock()
    {
        var clock = new DeadlineClock();
        var writeStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowWrite = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var storage = new StorageFaultControl
        {
            BeforeWrite = () =>
            {
                writeStarted.TrySetResult();
                return allowWrite.Task;
            }
        };
        await using var cluster = CreateClusterBuilder(clock, storage).Build();
        await cluster.DeployAsync();
        var key = Guid.NewGuid().ToString();
        var holder = cluster.Client.GetConcurrencyLimiter(key);
        using var capture = new MetricCapture(MetricCapture.WriteDuration);
        var reset = holder.ResetAsync().AsTask();
        try
        {
            await writeStarted.Task.WaitAsync(TestTimeout);
            var grain = cluster.Client.GetGrain<IConcurrencyLimiterGrain>(key).AsReference<IBoundedRateLimiterGrain>();
            var waiting = grain.AcquireWithDeadlineAsync(PermitCount, AcquisitionBudget);
            await clock.DeadlineScheduled.Task.WaitAsync(TestTimeout);
            clock.Advance(AcquisitionBudget);

            var exception = await Should.ThrowAsync<TimeoutException>(() => waiting.WaitAsync(TestTimeout));
            exception.Message.ShouldBe(DeadlineMessage);
            reset.IsCompleted.ShouldBeFalse();
        }
        finally
        {
            allowWrite.TrySetResult();
            await reset.WaitAsync(TestTimeout);
        }
        capture.Measurements.ShouldContain(item => item.Value == AcquisitionBudget.TotalSeconds);
    }

    [Test]
    public async Task FailedTimerWriteRetainsDirtyStateAndRetriesOnControlledClock()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var faults = new StorageFaultControl();
        await using var cluster = CreateClusterBuilder(clock, faults).Build();
        await cluster.DeployAsync();
        using var capture = new MetricCapture();
        var holder = cluster.Client.GetConcurrencyLimiter(Guid.NewGuid().ToString(),
            new ConcurrencyLimiterOptions { PermitLimit = PermitCount });
        await using var lease = await holder.AcquireAndConfigureAsync();
        var writes = Volatile.Read(ref faults.SuccessfulWrites);
        Volatile.Write(ref faults.FailWrites, true);
        clock.Advance(Advance);
        using var deadline = new CancellationTokenSource(TestTimeout);
        while (Volatile.Read(ref faults.FailedWrites) == default(int))
            await Task.Delay(PollMilliseconds, deadline.Token);
        Volatile.Write(ref faults.FailWrites, false);
        while (Volatile.Read(ref faults.SuccessfulWrites) <= writes)
        {
            clock.Advance(Advance);
            await Task.Delay(PollMilliseconds, deadline.Token);
        }
        capture.Measurements.ShouldContain(item => item.Name == MetricCapture.WriteCounter
            && Equals(item.Tags[MetricCapture.OutcomeTag], FailedOutcome));
        (await holder.GetStatisticsAsync())!.CurrentAvailablePermits.ShouldBe(default(int));
    }

    private static InProcessTestClusterBuilder CreateClusterBuilder(TimeProvider clock, StorageFaultControl? faults = null)
    {
        var builder = new InProcessTestClusterBuilder(SiloCount);
        builder.ConfigureSilo((_, silo) =>
        {
            new TestSiloConfigurations().Configure(silo);
            silo.Services.UseTimeProviderForBackgroundAreas(TimeProvider.System);
            silo.Services.AddKeyedSingleton(TimeProviderNames.Grains, clock);
            if (faults is null)
                return;

            var storage = silo.Services.Last(descriptor => descriptor.ServiceType == typeof(IGrainStorage)
                && descriptor.IsKeyedService && Equals(descriptor.ServiceKey, RateLimiterStorageNames.StorageProviderName));
            silo.Services.Remove(storage);
            silo.Services.AddKeyedSingleton<IGrainStorage>(RateLimiterStorageNames.StorageProviderName,
                (services, key) => new FaultingGrainStorage((IGrainStorage)storage.KeyedImplementationFactory!(services, key), faults));
        });
        return builder;
    }

    private sealed class DeadlineClock() : FakeTimeProvider(DateTimeOffset.UnixEpoch)
    {
        internal TaskCompletionSource DeadlineScheduled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = base.CreateTimer(callback, state, dueTime, period);
            if (dueTime == AcquisitionBudget && period == Timeout.InfiniteTimeSpan)
                DeadlineScheduled.TrySetResult();
            return timer;
        }
    }
}
