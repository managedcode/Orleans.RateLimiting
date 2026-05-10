using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;
using Orleans.Runtime;

namespace ManagedCode.Orleans.RateLimiting.Tests;

[ClassDataSource<TestClusterApplication>(Shared = SharedType.PerTestSession)]
public class RateLimiterStressPersistenceTests
{
    private const int AttemptCount = 100;
    private const int HalfAttemptCount = AttemptCount / 2;
    private const int ManagementGrainKey = 0;
    private const int QueueLimit = 0;
    private const int SegmentsPerWindow = 2;
    private const int WindowMinutes = 10;

    private static readonly TimeSpan ActivationCollectionAge = TimeSpan.Zero;
    private static readonly TimeSpan FixedWindow = TimeSpan.FromMinutes(WindowMinutes);

    private readonly TestClusterApplication _testApp;

    public RateLimiterStressPersistenceTests(TestClusterApplication testApp)
    {
        _testApp = testApp;
    }

    [Test]
    public async Task FixedWindowAllowsExactlyOneHundredPermitsAcrossForcedActivationCollection()
    {
        var rateLimiter = _testApp.Cluster.Client.GetFixedWindowRateLimiter($"{nameof(FixedWindowAllowsExactlyOneHundredPermitsAcrossForcedActivationCollection)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = false,
            PermitLimit = AttemptCount,
            QueueLimit = QueueLimit,
            Window = FixedWindow
        });

        await AssertOneHundredPermitsAcrossForcedActivationCollectionAsync(rateLimiter);
    }

    [Test]
    public async Task SlidingWindowAllowsExactlyOneHundredPermitsAcrossForcedActivationCollection()
    {
        var rateLimiter = _testApp.Cluster.Client.GetSlidingWindowRateLimiter($"{nameof(SlidingWindowAllowsExactlyOneHundredPermitsAcrossForcedActivationCollection)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new SlidingWindowRateLimiterOptions
        {
            AutoReplenishment = false,
            PermitLimit = AttemptCount,
            QueueLimit = QueueLimit,
            SegmentsPerWindow = SegmentsPerWindow,
            Window = FixedWindow
        });

        await AssertOneHundredPermitsAcrossForcedActivationCollectionAsync(rateLimiter);
    }

    [Test]
    public async Task TokenBucketAllowsExactlyOneHundredPermitsAcrossForcedActivationCollection()
    {
        var rateLimiter = _testApp.Cluster.Client.GetTokenBucketRateLimiter($"{nameof(TokenBucketAllowsExactlyOneHundredPermitsAcrossForcedActivationCollection)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new TokenBucketRateLimiterOptions
        {
            AutoReplenishment = false,
            TokenLimit = AttemptCount,
            TokensPerPeriod = AttemptCount,
            QueueLimit = QueueLimit,
            ReplenishmentPeriod = FixedWindow
        });

        await AssertOneHundredPermitsAcrossForcedActivationCollectionAsync(rateLimiter);
    }

    [Test]
    public async Task ConcurrencyAllowsExactlyOneHundredActivePermitsAcrossForcedActivationCollection()
    {
        var rateLimiter = _testApp.Cluster.Client.GetConcurrencyLimiter($"{nameof(ConcurrencyAllowsExactlyOneHundredActivePermitsAcrossForcedActivationCollection)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new ConcurrencyLimiterOptions
        {
            PermitLimit = AttemptCount,
            QueueLimit = QueueLimit
        });

        var firstHalf = await AcquireManyAsync(rateLimiter, HalfAttemptCount);
        firstHalf.ShouldAllBe(lease => lease.IsAcquired);

        await ForceActivationCollectionAsync();

        var secondHalf = await AcquireManyAsync(rateLimiter, HalfAttemptCount);
        secondHalf.ShouldAllBe(lease => lease.IsAcquired);

        var rejectedLeases = await AcquireManyAsync(rateLimiter, AttemptCount);
        rejectedLeases.ShouldAllBe(lease => !lease.IsAcquired);

        await DisposeAllAsync(rejectedLeases);
        await DisposeAllAsync(secondHalf);
        await DisposeAllAsync(firstHalf);
    }

    private static async Task<OrleansRateLimitLease[]> AcquireManyAsync(ILimiterHolder rateLimiter, int attemptCount)
    {
        return await Task.WhenAll(Enumerable.Range(0, attemptCount).Select(_ => rateLimiter.AcquireAsync()));
    }

    private static async Task DisposeAllAsync(IEnumerable<OrleansRateLimitLease> leases)
    {
        foreach (var lease in leases)
            await lease.DisposeAsync();
    }

    private async Task AssertOneHundredPermitsAcrossForcedActivationCollectionAsync(ILimiterHolder rateLimiter)
    {
        var firstHalf = await AcquireManyAsync(rateLimiter, HalfAttemptCount);
        firstHalf.ShouldAllBe(lease => lease.IsAcquired);
        await DisposeAllAsync(firstHalf);

        await ForceActivationCollectionAsync();

        var secondHalf = await AcquireManyAsync(rateLimiter, HalfAttemptCount);
        secondHalf.ShouldAllBe(lease => lease.IsAcquired);
        await DisposeAllAsync(secondHalf);

        var rejectedLeases = await AcquireManyAsync(rateLimiter, AttemptCount);
        rejectedLeases.ShouldAllBe(lease => !lease.IsAcquired);
        await DisposeAllAsync(rejectedLeases);
    }

    private async Task ForceActivationCollectionAsync()
    {
        var managementGrain = _testApp.Cluster.Client.GetGrain<IManagementGrain>(ManagementGrainKey);
        await managementGrain.ForceActivationCollection(ActivationCollectionAge);
    }
}
