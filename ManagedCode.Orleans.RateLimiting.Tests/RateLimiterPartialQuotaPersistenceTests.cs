using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;
using Orleans.Runtime;

namespace ManagedCode.Orleans.RateLimiting.Tests;

[ClassDataSource<TestClusterApplication>(Shared = SharedType.PerTestSession)]
public class RateLimiterPartialQuotaPersistenceTests
{
    private const int ManagementGrainKey = 0;
    private const int PermitLimit = 4;
    private const int QueueLimit = 0;
    private const int SegmentsPerWindow = 2;
    private const int WindowMinutes = 10;

    private static readonly TimeSpan ActivationCollectionAge = TimeSpan.Zero;
    private static readonly TimeSpan FixedWindow = TimeSpan.FromMinutes(WindowMinutes);

    private readonly TestClusterApplication _testApp;

    public RateLimiterPartialQuotaPersistenceTests(TestClusterApplication testApp)
    {
        _testApp = testApp;
    }

    [Test]
    public async Task FixedWindowSurvivesForcedActivationCollectionWithoutExtraPermits()
    {
        var rateLimiter = _testApp.Cluster.Client.GetFixedWindowRateLimiter($"{nameof(FixedWindowSurvivesForcedActivationCollectionWithoutExtraPermits)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = false,
            PermitLimit = PermitLimit,
            QueueLimit = QueueLimit,
            Window = FixedWindow
        });

        await AssertPartialQuotaSurvivesForcedActivationCollectionAsync(rateLimiter);
    }

    [Test]
    public async Task SlidingWindowSurvivesForcedActivationCollectionWithoutExtraPermits()
    {
        var rateLimiter = _testApp.Cluster.Client.GetSlidingWindowRateLimiter($"{nameof(SlidingWindowSurvivesForcedActivationCollectionWithoutExtraPermits)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new SlidingWindowRateLimiterOptions
        {
            AutoReplenishment = false,
            PermitLimit = PermitLimit,
            QueueLimit = QueueLimit,
            SegmentsPerWindow = SegmentsPerWindow,
            Window = FixedWindow
        });

        await AssertPartialQuotaSurvivesForcedActivationCollectionAsync(rateLimiter);
    }

    [Test]
    public async Task TokenBucketSurvivesForcedActivationCollectionWithoutExtraPermits()
    {
        var rateLimiter = _testApp.Cluster.Client.GetTokenBucketRateLimiter($"{nameof(TokenBucketSurvivesForcedActivationCollectionWithoutExtraPermits)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new TokenBucketRateLimiterOptions
        {
            AutoReplenishment = false,
            TokenLimit = PermitLimit,
            TokensPerPeriod = PermitLimit,
            QueueLimit = QueueLimit,
            ReplenishmentPeriod = FixedWindow
        });

        await AssertPartialQuotaSurvivesForcedActivationCollectionAsync(rateLimiter);
    }

    [Test]
    public async Task ConcurrencySurvivesForcedActivationCollectionWithoutExtraPermits()
    {
        var rateLimiter = _testApp.Cluster.Client.GetConcurrencyLimiter($"{nameof(ConcurrencySurvivesForcedActivationCollectionWithoutExtraPermits)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new ConcurrencyLimiterOptions
        {
            PermitLimit = PermitLimit,
            QueueLimit = QueueLimit
        });

        var firstHeldLease = await rateLimiter.AcquireAsync();
        var secondHeldLease = await rateLimiter.AcquireAsync();
        firstHeldLease.IsAcquired.ShouldBeTrue();
        secondHeldLease.IsAcquired.ShouldBeTrue();

        await ForceActivationCollectionAsync();

        var thirdLease = await rateLimiter.AcquireAsync();
        var fourthLease = await rateLimiter.AcquireAsync();
        thirdLease.IsAcquired.ShouldBeTrue();
        fourthLease.IsAcquired.ShouldBeTrue();

        await using (var rejectedLease = await rateLimiter.AcquireAsync())
            rejectedLease.IsAcquired.ShouldBeFalse();

        await thirdLease.DisposeAsync();
        await fourthLease.DisposeAsync();
        await firstHeldLease.DisposeAsync();
        await secondHeldLease.DisposeAsync();
    }

    private async Task AssertPartialQuotaSurvivesForcedActivationCollectionAsync(ILimiterHolder rateLimiter)
    {
        await using (var firstLease = await rateLimiter.AcquireAsync())
            firstLease.IsAcquired.ShouldBeTrue();

        await using (var secondLease = await rateLimiter.AcquireAsync())
            secondLease.IsAcquired.ShouldBeTrue();

        await ForceActivationCollectionAsync();

        await using (var thirdLease = await rateLimiter.AcquireAsync())
            thirdLease.IsAcquired.ShouldBeTrue();

        await using (var fourthLease = await rateLimiter.AcquireAsync())
            fourthLease.IsAcquired.ShouldBeTrue();

        await using var rejectedLease = await rateLimiter.AcquireAsync();
        rejectedLease.IsAcquired.ShouldBeFalse();
    }

    private async Task ForceActivationCollectionAsync()
    {
        var managementGrain = _testApp.Cluster.Client.GetGrain<IManagementGrain>(ManagementGrainKey);
        await managementGrain.ForceActivationCollection(ActivationCollectionAge);
    }
}
