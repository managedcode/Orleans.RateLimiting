using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;
using Orleans;
using Orleans.Runtime;

namespace ManagedCode.Orleans.RateLimiting.Tests;

[ClassDataSource<TestClusterApplication>(Shared = SharedType.PerTestSession)]
public class RateLimiterConfigurationAndSnapshotPersistenceTests
{
    private const int ExpectedAvailablePermits = PermitLimit - PermitCount;
    private const int ManagementGrainKey = 0;
    private const int PermitCount = 2;
    private const int PermitLimit = 5;
    private const int QueueLimit = 0;
    private const int SegmentsPerWindow = 2;
    private const int TokensPerPeriod = 1;
    private const int WindowMinutes = 10;

    private static readonly TimeSpan ActivationCollectionAge = TimeSpan.Zero;
    private static readonly TimeSpan FixedWindow = TimeSpan.FromMinutes(WindowMinutes);

    private readonly TestClusterApplication _testApp;

    public RateLimiterConfigurationAndSnapshotPersistenceTests(TestClusterApplication testApp)
    {
        _testApp = testApp;
    }

    [Test]
    public async Task FixedWindowConfigurationAndCurrentQuotaValueSurviveForcedActivationCollection()
    {
        var rateLimiter = _testApp.Cluster.Client.GetFixedWindowRateLimiter($"{nameof(FixedWindowConfigurationAndCurrentQuotaValueSurviveForcedActivationCollection)}-{Guid.NewGuid():N}");
        var options = new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = false,
            PermitLimit = PermitLimit,
            QueueLimit = QueueLimit,
            Window = FixedWindow
        };

        await rateLimiter.Configure(options);
        await AcquireAndDisposeAsync(rateLimiter);
        await ForceActivationCollectionAsync();

        var configuration = await rateLimiter.GetConfiguration();
        configuration.PermitLimit.ShouldBe(PermitLimit);
        configuration.QueueLimit.ShouldBe(QueueLimit);
        configuration.Window.ShouldBe(FixedWindow);

        await AssertCurrentAvailablePermitsAsync(rateLimiter);
    }

    [Test]
    public async Task SlidingWindowConfigurationAndCurrentQuotaValueSurviveForcedActivationCollection()
    {
        var rateLimiter = _testApp.Cluster.Client.GetSlidingWindowRateLimiter($"{nameof(SlidingWindowConfigurationAndCurrentQuotaValueSurviveForcedActivationCollection)}-{Guid.NewGuid():N}");
        var options = new SlidingWindowRateLimiterOptions
        {
            AutoReplenishment = false,
            PermitLimit = PermitLimit,
            QueueLimit = QueueLimit,
            SegmentsPerWindow = SegmentsPerWindow,
            Window = FixedWindow
        };

        await rateLimiter.Configure(options);
        await AcquireAndDisposeAsync(rateLimiter);
        await ForceActivationCollectionAsync();

        var configuration = await rateLimiter.GetConfiguration();
        configuration.PermitLimit.ShouldBe(PermitLimit);
        configuration.QueueLimit.ShouldBe(QueueLimit);
        configuration.SegmentsPerWindow.ShouldBe(SegmentsPerWindow);
        configuration.Window.ShouldBe(FixedWindow);

        await AssertCurrentAvailablePermitsAsync(rateLimiter);
    }

    [Test]
    public async Task TokenBucketConfigurationAndCurrentQuotaValueSurviveForcedActivationCollection()
    {
        var rateLimiter = _testApp.Cluster.Client.GetTokenBucketRateLimiter($"{nameof(TokenBucketConfigurationAndCurrentQuotaValueSurviveForcedActivationCollection)}-{Guid.NewGuid():N}");
        var options = new TokenBucketRateLimiterOptions
        {
            AutoReplenishment = false,
            QueueLimit = QueueLimit,
            ReplenishmentPeriod = FixedWindow,
            TokenLimit = PermitLimit,
            TokensPerPeriod = TokensPerPeriod
        };

        await rateLimiter.Configure(options);
        await AcquireAndDisposeAsync(rateLimiter);
        await ForceActivationCollectionAsync();

        var configuration = await rateLimiter.GetConfiguration();
        configuration.QueueLimit.ShouldBe(QueueLimit);
        configuration.ReplenishmentPeriod.ShouldBe(FixedWindow);
        configuration.TokenLimit.ShouldBe(PermitLimit);
        configuration.TokensPerPeriod.ShouldBe(TokensPerPeriod);

        await AssertCurrentAvailablePermitsAsync(rateLimiter);
    }

    [Test]
    public async Task ConcurrencyConfigurationCurrentQuotaValueAndActiveLeasesSurviveForcedActivationCollection()
    {
        var rateLimiter = _testApp.Cluster.Client.GetConcurrencyLimiter($"{nameof(ConcurrencyConfigurationCurrentQuotaValueAndActiveLeasesSurviveForcedActivationCollection)}-{Guid.NewGuid():N}");
        var options = new ConcurrencyLimiterOptions
        {
            PermitLimit = PermitLimit,
            QueueLimit = QueueLimit
        };

        await rateLimiter.Configure(options);
        var heldLease = await rateLimiter.AcquireAsync(PermitCount);
        heldLease.IsAcquired.ShouldBeTrue();

        await ForceActivationCollectionAsync();

        var configuration = await rateLimiter.GetConfiguration();
        configuration.PermitLimit.ShouldBe(PermitLimit);
        configuration.QueueLimit.ShouldBe(QueueLimit);

        await AssertCurrentAvailablePermitsAsync(rateLimiter);

        await heldLease.DisposeAsync();
    }

    private static async Task AcquireAndDisposeAsync<TGrain, TOptions>(BaseRateLimiterHolder<TGrain, TOptions> rateLimiter)
        where TGrain : IGrainWithStringKey, IRateLimiterGrainWithConfiguration<TOptions>
        where TOptions : class
    {
        await using var acquiredLease = await rateLimiter.AcquireAsync(PermitCount);
        acquiredLease.IsAcquired.ShouldBeTrue();
    }

    private static async Task AssertCurrentAvailablePermitsAsync<TGrain, TOptions>(BaseRateLimiterHolder<TGrain, TOptions> rateLimiter)
        where TGrain : IGrainWithStringKey, IRateLimiterGrainWithConfiguration<TOptions>
        where TOptions : class
    {
        var statistics = await rateLimiter.GetStatisticsAsync();
        statistics.ShouldNotBeNull();
        statistics.CurrentAvailablePermits.ShouldBe(ExpectedAvailablePermits);
    }

    private async Task ForceActivationCollectionAsync()
    {
        var managementGrain = _testApp.Cluster.Client.GetGrain<IManagementGrain>(ManagementGrainKey);
        await managementGrain.ForceActivationCollection(ActivationCollectionAge);
    }
}
