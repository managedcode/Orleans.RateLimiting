using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Server.Grains;
using ManagedCode.Orleans.RateLimiting.Server.Options;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OptionsFactory = Microsoft.Extensions.Options.Options;
using Microsoft.Extensions.Time.Testing;
using Orleans.TestingHost;
using Orleans.Storage;
using ManagedCode.Orleans.RateLimiting.Server;

namespace ManagedCode.Orleans.RateLimiting.Tests;

public class ControlledClockTests
{
    private const string FailedOutcome = "failed";
    private const short SiloCount = 1;
    private const int PermitCount = 1;
    private const int AdvanceMilliseconds = 100;
    private const int PollMilliseconds = 10;
    private const int TimeoutSeconds = 10;
    private static readonly TimeSpan Advance = TimeSpan.FromMilliseconds(AdvanceMilliseconds);

    [Test]
    public async Task SnapshotTimestampsFollowTheInjectedClock()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var state = new FaultingPersistentState<ConcurrencyLimiterOptions>();
        using var grain = new ClockGrain(state, clock);
        await grain.ResetAsync();
        state.State.UpdatedAtUtc.ShouldBe(clock.GetUtcNow());
        clock.Advance(Advance);
        await grain.ResetAsync();
        state.State.UpdatedAtUtc.ShouldBe(clock.GetUtcNow());
    }

    [Test]
    public async Task FailedTimerWriteRetainsDirtyStateAndRetriesOnControlledClock()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var faults = new StorageFaultControl();
        var builder = new InProcessTestClusterBuilder(SiloCount);
        builder.ConfigureSilo((_, silo) =>
        {
            new TestSiloConfigurations().Configure(silo);
            silo.Services.AddSingleton<TimeProvider>(clock);
            silo.Services.UseTimeProviderForBackgroundAreas(TimeProvider.System);
            var storage = silo.Services.Last(descriptor => descriptor.ServiceType == typeof(IGrainStorage)
                && descriptor.IsKeyedService && Equals(descriptor.ServiceKey, RateLimiterStorageNames.StorageProviderName));
            silo.Services.Remove(storage);
            silo.Services.AddKeyedSingleton<IGrainStorage>(RateLimiterStorageNames.StorageProviderName,
                (services, key) => new FaultingGrainStorage((IGrainStorage)storage.KeyedImplementationFactory!(services, key), faults));
        });
        await using var cluster = builder.Build();
        await cluster.DeployAsync();
        using var capture = new MetricCapture();
        var holder = cluster.Client.GetConcurrencyLimiter(Guid.NewGuid().ToString(),
            new ConcurrencyLimiterOptions { PermitLimit = PermitCount });
        await using var lease = await holder.AcquireAndConfigureAsync();
        var writes = Volatile.Read(ref faults.SuccessfulWrites);
        Volatile.Write(ref faults.FailWrites, true);
        clock.Advance(Advance);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
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

    private sealed class ClockGrain(FaultingPersistentState<ConcurrencyLimiterOptions> state, TimeProvider clock)
        : ConcurrencyLimiterGrain(NullLogger<ConcurrencyLimiterGrain>.Instance,
            OptionsFactory.Create(new ConcurrencyLimiterOptions { PermitLimit = PermitCount }),
            OptionsFactory.Create(new RateLimiterPersistenceOptions { StateFlushPeriod = TimeSpan.Zero }), state)
    {
        protected override TimeProvider Clock => clock;
    }
}
