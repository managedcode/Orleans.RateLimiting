using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Server.Grains;
using ManagedCode.Orleans.RateLimiting.Server.Options;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;
using Microsoft.Extensions.Logging.Abstractions;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace ManagedCode.Orleans.RateLimiting.Tests;

public class TokenBucketSnapshotBoundaryTests
{
    private const int Limit = 10;
    private const int SavedPermits = 3;
    private const int TokensPerPeriod = 2;
    private const int SingleTick = 1;
    private const int ElapsedTicks = 3;
    private const int ExpectedPartial = 9;
    private const int NoPermits = 0;
    private static readonly DateTimeOffset SavedAt = DateTimeOffset.UnixEpoch;

    [Test]
    public void LongOutageCannotOverflowRestoredPermits()
    {
        using var grain = CreateGrain(int.MaxValue);
        grain.Restore(DateTimeOffset.MinValue, NoPermits, DateTimeOffset.MaxValue).ShouldBe(Limit);
    }

    [Test]
    public void BackwardsClockDoesNotSubtractPermits()
    {
        using var grain = CreateGrain(TokensPerPeriod);
        grain.Restore(SavedAt, SavedPermits, SavedAt.AddTicks(-ElapsedTicks)).ShouldBe(SavedPermits);
    }

    [Test]
    public void PartialPeriodsRestoreExactlyTheElapsedQuota()
    {
        using var grain = CreateGrain(TokensPerPeriod);
        grain.Restore(SavedAt, SavedPermits, SavedAt.AddTicks(ElapsedTicks)).ShouldBe(ExpectedPartial);
    }

    private static TestTokenGrain CreateGrain(int tokens) => new(new TokenBucketRateLimiterOptions
    {
        AutoReplenishment = true,
        TokenLimit = Limit,
        TokensPerPeriod = tokens,
        ReplenishmentPeriod = TimeSpan.FromTicks(SingleTick)
    });

    private sealed class TestTokenGrain(TokenBucketRateLimiterOptions options)
        : TokenBucketRateLimiterGrain(NullLogger<TokenBucketRateLimiterGrain>.Instance,
            OptionsFactory.Create(options), OptionsFactory.Create(new RateLimiterPersistenceOptions()), new FaultingPersistentState<TokenBucketRateLimiterOptions>())
    {
        public int Restore(DateTimeOffset saved, int permits, DateTimeOffset now) => GetRestoredAvailablePermits(saved, permits, now);
    }
}
