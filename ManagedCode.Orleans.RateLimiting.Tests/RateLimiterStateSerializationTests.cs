using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using ManagedCode.Orleans.RateLimiting.Server.Grains;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace ManagedCode.Orleans.RateLimiting.Tests;

public class RateLimiterStateSerializationTests
{
    private const bool AutoReplenishment = false;
    private const int ActiveLeasePermitCount = 3;
    private const int CurrentAvailablePermits = 37;
    private const int PermitLimit = 100;
    private const int QueueLimit = 4;
    private const int SegmentsPerWindow = 5;
    private const int StateDay = 10;
    private const int StateHour = 12;
    private const int StateMinute = 34;
    private const int StateMonth = 5;
    private const int StateSecond = 56;
    private const int StateYear = 2026;
    private const int TokenLimit = PermitLimit;
    private const int TokensPerPeriod = 9;
    private const int TotalFailedLeases = 7;
    private const int TotalSuccessfulLeases = 63;
    private const int WindowMinutes = 10;
    private const string ActiveLeaseIdText = "6D5A8D56-98C4-48D2-97F9-23F63769C8E2";

    private static readonly Guid ActiveLeaseId = Guid.Parse(ActiveLeaseIdText);
    private static readonly TimeSpan FixedWindow = TimeSpan.FromMinutes(WindowMinutes);
    private static readonly DateTimeOffset StateUpdatedAtUtc = new(StateYear, StateMonth, StateDay, StateHour, StateMinute, StateSecond, TimeSpan.Zero);

    [Test]
    public void FixedWindowStateRoundTripsConfigurationAndCurrentQuotaValueThroughOrleansSerializer()
    {
        var originalState = CreateState(
            new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = AutoReplenishment,
                PermitLimit = PermitLimit,
                QueueLimit = QueueLimit,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                Window = FixedWindow
            });

        var state = RoundTrip(originalState);

        AssertCommonState(state);
        state.ActiveLeases.ShouldBeEmpty();
        state.Options.ShouldNotBeNull();
        state.Options.AutoReplenishment.ShouldBe(AutoReplenishment);
        state.Options.PermitLimit.ShouldBe(PermitLimit);
        state.Options.QueueLimit.ShouldBe(QueueLimit);
        state.Options.QueueProcessingOrder.ShouldBe(QueueProcessingOrder.NewestFirst);
        state.Options.Window.ShouldBe(FixedWindow);
    }

    [Test]
    public void SlidingWindowStateRoundTripsConfigurationAndCurrentQuotaValueThroughOrleansSerializer()
    {
        var originalState = CreateState(
            new SlidingWindowRateLimiterOptions
            {
                AutoReplenishment = AutoReplenishment,
                PermitLimit = PermitLimit,
                QueueLimit = QueueLimit,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                SegmentsPerWindow = SegmentsPerWindow,
                Window = FixedWindow
            });

        var state = RoundTrip(originalState);

        AssertCommonState(state);
        state.ActiveLeases.ShouldBeEmpty();
        state.Options.ShouldNotBeNull();
        state.Options.AutoReplenishment.ShouldBe(AutoReplenishment);
        state.Options.PermitLimit.ShouldBe(PermitLimit);
        state.Options.QueueLimit.ShouldBe(QueueLimit);
        state.Options.QueueProcessingOrder.ShouldBe(QueueProcessingOrder.NewestFirst);
        state.Options.SegmentsPerWindow.ShouldBe(SegmentsPerWindow);
        state.Options.Window.ShouldBe(FixedWindow);
    }

    [Test]
    public void TokenBucketStateRoundTripsConfigurationAndCurrentQuotaValueThroughOrleansSerializer()
    {
        var originalState = CreateState(
            new TokenBucketRateLimiterOptions
            {
                AutoReplenishment = AutoReplenishment,
                QueueLimit = QueueLimit,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                ReplenishmentPeriod = FixedWindow,
                TokenLimit = TokenLimit,
                TokensPerPeriod = TokensPerPeriod
            });

        var state = RoundTrip(originalState);

        AssertCommonState(state);
        state.ActiveLeases.ShouldBeEmpty();
        state.Options.ShouldNotBeNull();
        state.Options.AutoReplenishment.ShouldBe(AutoReplenishment);
        state.Options.QueueLimit.ShouldBe(QueueLimit);
        state.Options.QueueProcessingOrder.ShouldBe(QueueProcessingOrder.NewestFirst);
        state.Options.ReplenishmentPeriod.ShouldBe(FixedWindow);
        state.Options.TokenLimit.ShouldBe(TokenLimit);
        state.Options.TokensPerPeriod.ShouldBe(TokensPerPeriod);
    }

    [Test]
    public void ConcurrencyStateRoundTripsConfigurationCurrentQuotaValueAndActiveLeasesThroughOrleansSerializer()
    {
        var originalState = CreateState(
            new ConcurrencyLimiterOptions
            {
                PermitLimit = PermitLimit,
                QueueLimit = QueueLimit,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst
            },
            includeActiveLease: true);

        var state = RoundTrip(originalState);

        AssertCommonState(state);
        state.Options.ShouldNotBeNull();
        state.Options.PermitLimit.ShouldBe(PermitLimit);
        state.Options.QueueLimit.ShouldBe(QueueLimit);
        state.Options.QueueProcessingOrder.ShouldBe(QueueProcessingOrder.NewestFirst);

        var activeLease = state.ActiveLeases.Single();
        activeLease.LeaseId.ShouldBe(ActiveLeaseId);
        activeLease.PermitCount.ShouldBe(ActiveLeasePermitCount);
    }

    private static void AssertCommonState<TOptions>(RateLimiterGrainState<TOptions> state)
        where TOptions : class
    {
        state.HasConfiguration.ShouldBeTrue();
        state.HasSnapshot.ShouldBeTrue();
        state.CurrentAvailablePermits.ShouldBe(CurrentAvailablePermits);
        state.TotalFailedLeases.ShouldBe(TotalFailedLeases);
        state.TotalSuccessfulLeases.ShouldBe(TotalSuccessfulLeases);
        state.UpdatedAtUtc.ShouldBe(StateUpdatedAtUtc);
    }

    private static RateLimiterGrainState<TOptions> CreateState<TOptions>(TOptions options, bool includeActiveLease = false)
        where TOptions : class
    {
        var state = new RateLimiterGrainState<TOptions>
        {
            HasConfiguration = true,
            HasSnapshot = true,
            CurrentAvailablePermits = CurrentAvailablePermits,
            Options = options,
            TotalFailedLeases = TotalFailedLeases,
            TotalSuccessfulLeases = TotalSuccessfulLeases,
            UpdatedAtUtc = StateUpdatedAtUtc
        };

        if (includeActiveLease)
            state.ActiveLeases.Add(new RateLimiterLeaseState(ActiveLeaseId, ActiveLeasePermitCount));

        return state;
    }

    private static RateLimiterGrainState<TOptions> RoundTrip<TOptions>(RateLimiterGrainState<TOptions> state)
        where TOptions : class
    {
        using var serviceProvider = CreateServiceProvider();
        var serializer = serviceProvider.GetRequiredService<Serializer<RateLimiterGrainState<TOptions>>>();

        var serializedState = serializer.SerializeToArray(state);
        serializedState.Length.ShouldBeGreaterThan(0);

        var deserializedState = serializer.Deserialize(serializedState);
        deserializedState.ShouldNotBeNull();
        return deserializedState;
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSerializer(serializerBuilder =>
        {
            serializerBuilder.AddAssembly(typeof(RateLimiterConfig).Assembly);
            serializerBuilder.AddAssembly(typeof(RateLimiterGrainState<>).Assembly);
        });

        return services.BuildServiceProvider();
    }
}
