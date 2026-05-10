using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;
using Orleans.Runtime;

namespace ManagedCode.Orleans.RateLimiting.Tests;

[ClassDataSource<TestClusterApplication>(Shared = SharedType.PerTestSession)]
public class RateLimiterDeleteStatePersistenceTests
{
    private const int DefaultPermitLimit = 10;
    private const int ManagementGrainKey = 0;
    private const int PermitLimit = 1;
    private const int QueueLimit = 0;
    private const int SegmentsPerWindow = 2;
    private const int WindowMinutes = 10;

    private static readonly TimeSpan ActivationCollectionAge = TimeSpan.Zero;
    private static readonly TimeSpan FixedWindow = TimeSpan.FromMinutes(WindowMinutes);

    private readonly TestClusterApplication _testApp;

    public RateLimiterDeleteStatePersistenceTests(TestClusterApplication testApp)
    {
        _testApp = testApp;
    }

    [Test]
    public async Task FixedWindowDeleteStateRemovesPersistedConfigurationAndQuota()
    {
        var rateLimiter = _testApp.Cluster.Client.GetFixedWindowRateLimiter($"{nameof(FixedWindowDeleteStateRemovesPersistedConfigurationAndQuota)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = false,
            PermitLimit = PermitLimit,
            QueueLimit = QueueLimit,
            Window = FixedWindow
        });

        await AssertReleasedLeaseDoesNotReturnQuotaAsync(rateLimiter);

        await rateLimiter.DeleteStateAsync();
        await ForceActivationCollectionAsync();

        var configuration = await rateLimiter.GetConfiguration();
        configuration.PermitLimit.ShouldBe(DefaultPermitLimit);

        await using var acquiredAfterDelete = await rateLimiter.AcquireAsync();
        acquiredAfterDelete.IsAcquired.ShouldBeTrue();
    }

    [Test]
    public async Task SlidingWindowDeleteStateRemovesPersistedConfigurationAndQuota()
    {
        var rateLimiter = _testApp.Cluster.Client.GetSlidingWindowRateLimiter($"{nameof(SlidingWindowDeleteStateRemovesPersistedConfigurationAndQuota)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new SlidingWindowRateLimiterOptions
        {
            AutoReplenishment = false,
            PermitLimit = PermitLimit,
            QueueLimit = QueueLimit,
            SegmentsPerWindow = SegmentsPerWindow,
            Window = FixedWindow
        });

        await AssertReleasedLeaseDoesNotReturnQuotaAsync(rateLimiter);

        await rateLimiter.DeleteStateAsync();
        await ForceActivationCollectionAsync();

        var configuration = await rateLimiter.GetConfiguration();
        configuration.PermitLimit.ShouldBe(DefaultPermitLimit);

        await using var acquiredAfterDelete = await rateLimiter.AcquireAsync();
        acquiredAfterDelete.IsAcquired.ShouldBeTrue();
    }

    [Test]
    public async Task TokenBucketDeleteStateRemovesPersistedConfigurationAndQuota()
    {
        var rateLimiter = _testApp.Cluster.Client.GetTokenBucketRateLimiter($"{nameof(TokenBucketDeleteStateRemovesPersistedConfigurationAndQuota)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new TokenBucketRateLimiterOptions
        {
            AutoReplenishment = false,
            QueueLimit = QueueLimit,
            ReplenishmentPeriod = FixedWindow,
            TokenLimit = PermitLimit,
            TokensPerPeriod = PermitLimit
        });

        await AssertReleasedLeaseDoesNotReturnQuotaAsync(rateLimiter);

        await rateLimiter.DeleteStateAsync();
        await ForceActivationCollectionAsync();

        var configuration = await rateLimiter.GetConfiguration();
        configuration.TokenLimit.ShouldBe(DefaultPermitLimit);

        await using var acquiredAfterDelete = await rateLimiter.AcquireAsync();
        acquiredAfterDelete.IsAcquired.ShouldBeTrue();
    }

    [Test]
    public async Task ConcurrencyDeleteStateRemovesPersistedConfigurationAndActiveLeases()
    {
        var rateLimiter = _testApp.Cluster.Client.GetConcurrencyLimiter($"{nameof(ConcurrencyDeleteStateRemovesPersistedConfigurationAndActiveLeases)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new ConcurrencyLimiterOptions
        {
            PermitLimit = PermitLimit,
            QueueLimit = QueueLimit
        });

        var heldLease = await rateLimiter.AcquireAsync();
        heldLease.IsAcquired.ShouldBeTrue();
        await AssertAcquireRejectedAsync(rateLimiter);

        await rateLimiter.DeleteStateAsync();
        await ForceActivationCollectionAsync();

        var configuration = await rateLimiter.GetConfiguration();
        configuration.PermitLimit.ShouldBe(DefaultPermitLimit);

        await using var acquiredAfterDelete = await rateLimiter.AcquireAsync();
        acquiredAfterDelete.IsAcquired.ShouldBeTrue();

        await heldLease.DisposeAsync();
    }

    private static async Task AssertAcquireRejectedAsync(ILimiterHolder rateLimiter)
    {
        await using var rejectedLease = await rateLimiter.AcquireAsync();
        rejectedLease.IsAcquired.ShouldBeFalse();
    }

    private static async Task AssertReleasedLeaseDoesNotReturnQuotaAsync(ILimiterHolder rateLimiter)
    {
        await using (var acquiredLease = await rateLimiter.AcquireAsync())
            acquiredLease.IsAcquired.ShouldBeTrue();

        await AssertAcquireRejectedAsync(rateLimiter);
    }

    private async Task ForceActivationCollectionAsync()
    {
        var managementGrain = _testApp.Cluster.Client.GetGrain<IManagementGrain>(ManagementGrainKey);
        await managementGrain.ForceActivationCollection(ActivationCollectionAge);
    }
}
