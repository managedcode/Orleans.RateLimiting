using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;
using Orleans.Runtime;

namespace ManagedCode.Orleans.RateLimiting.Tests;

[ClassDataSource<TestClusterApplication>(Shared = SharedType.PerTestSession)]
public class RateLimiterCorrectnessPersistenceTests
{
    private const int ExpandedPermitLimit = 2;
    private const int ManagementGrainKey = 0;
    private const int PermitLimit = 1;
    private const int QueueLimit = 0;
    private const int ReplenishmentDelayMilliseconds = 250;
    private const int ReplenishmentPeriodMilliseconds = 100;
    private const int SegmentsPerWindow = 2;
    private const int TokensPerPeriod = ExpandedPermitLimit;
    private const int WindowMinutes = 10;

    private static readonly TimeSpan ActivationCollectionAge = TimeSpan.Zero;
    private static readonly TimeSpan FixedWindow = TimeSpan.FromMinutes(WindowMinutes);
    private static readonly TimeSpan ReplenishmentDelay = TimeSpan.FromMilliseconds(ReplenishmentDelayMilliseconds);
    private static readonly TimeSpan ReplenishmentPeriod = TimeSpan.FromMilliseconds(ReplenishmentPeriodMilliseconds);

    private readonly TestClusterApplication _testApp;

    public RateLimiterCorrectnessPersistenceTests(TestClusterApplication testApp)
    {
        _testApp = testApp;
    }

    [Test]
    public async Task FixedWindowLeaseReleaseDoesNotReturnConsumedQuota()
    {
        var rateLimiter = _testApp.Cluster.Client.GetFixedWindowRateLimiter($"{nameof(FixedWindowLeaseReleaseDoesNotReturnConsumedQuota)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = false,
            PermitLimit = PermitLimit,
            QueueLimit = QueueLimit,
            Window = FixedWindow
        });

        await AssertReleasedLeaseDoesNotReturnQuotaAsync(rateLimiter);
    }

    [Test]
    public async Task SlidingWindowLeaseReleaseDoesNotReturnConsumedQuota()
    {
        var rateLimiter = _testApp.Cluster.Client.GetSlidingWindowRateLimiter($"{nameof(SlidingWindowLeaseReleaseDoesNotReturnConsumedQuota)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new SlidingWindowRateLimiterOptions
        {
            AutoReplenishment = false,
            PermitLimit = PermitLimit,
            QueueLimit = QueueLimit,
            SegmentsPerWindow = SegmentsPerWindow,
            Window = FixedWindow
        });

        await AssertReleasedLeaseDoesNotReturnQuotaAsync(rateLimiter);
    }

    [Test]
    public async Task TokenBucketLeaseReleaseDoesNotReturnConsumedQuota()
    {
        var rateLimiter = _testApp.Cluster.Client.GetTokenBucketRateLimiter($"{nameof(TokenBucketLeaseReleaseDoesNotReturnConsumedQuota)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new TokenBucketRateLimiterOptions
        {
            AutoReplenishment = false,
            QueueLimit = QueueLimit,
            ReplenishmentPeriod = FixedWindow,
            TokenLimit = PermitLimit,
            TokensPerPeriod = PermitLimit
        });

        await AssertReleasedLeaseDoesNotReturnQuotaAsync(rateLimiter);
    }

    [Test]
    public async Task ConcurrencyLeaseReleaseReturnsPermitAfterForcedActivationCollection()
    {
        var rateLimiter = _testApp.Cluster.Client.GetConcurrencyLimiter($"{nameof(ConcurrencyLeaseReleaseReturnsPermitAfterForcedActivationCollection)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new ConcurrencyLimiterOptions
        {
            PermitLimit = PermitLimit,
            QueueLimit = QueueLimit
        });

        var heldLease = await rateLimiter.AcquireAsync();
        heldLease.IsAcquired.ShouldBeTrue();

        await ForceActivationCollectionAsync();
        await AssertAcquireRejectedAsync(rateLimiter);

        await heldLease.DisposeAsync();
        await ForceActivationCollectionAsync();

        await using var acquiredAfterRelease = await rateLimiter.AcquireAsync();
        acquiredAfterRelease.IsAcquired.ShouldBeTrue();
    }

    [Test]
    public async Task SlidingWindowResetClearsPersistedQuota()
    {
        var rateLimiter = _testApp.Cluster.Client.GetSlidingWindowRateLimiter($"{nameof(SlidingWindowResetClearsPersistedQuota)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new SlidingWindowRateLimiterOptions
        {
            AutoReplenishment = false,
            PermitLimit = PermitLimit,
            QueueLimit = QueueLimit,
            SegmentsPerWindow = SegmentsPerWindow,
            Window = FixedWindow
        });

        await AssertResetClearsQuotaAsync(rateLimiter);
    }

    [Test]
    public async Task TokenBucketResetClearsPersistedQuota()
    {
        var rateLimiter = _testApp.Cluster.Client.GetTokenBucketRateLimiter($"{nameof(TokenBucketResetClearsPersistedQuota)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new TokenBucketRateLimiterOptions
        {
            AutoReplenishment = false,
            QueueLimit = QueueLimit,
            ReplenishmentPeriod = FixedWindow,
            TokenLimit = PermitLimit,
            TokensPerPeriod = PermitLimit
        });

        await AssertResetClearsQuotaAsync(rateLimiter);
    }

    [Test]
    public async Task ConcurrencyResetClearsPersistedActiveLeases()
    {
        var rateLimiter = _testApp.Cluster.Client.GetConcurrencyLimiter($"{nameof(ConcurrencyResetClearsPersistedActiveLeases)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new ConcurrencyLimiterOptions
        {
            PermitLimit = PermitLimit,
            QueueLimit = QueueLimit
        });

        var heldLease = await rateLimiter.AcquireAsync();
        heldLease.IsAcquired.ShouldBeTrue();

        await rateLimiter.ResetAsync();
        await ForceActivationCollectionAsync();

        await using var acquiredAfterReset = await rateLimiter.AcquireAsync();
        acquiredAfterReset.IsAcquired.ShouldBeTrue();

        await heldLease.DisposeAsync();
    }

    [Test]
    public async Task FixedWindowReconfigurePersistsNewLimitAndResetsQuota()
    {
        var rateLimiter = _testApp.Cluster.Client.GetFixedWindowRateLimiter($"{nameof(FixedWindowReconfigurePersistsNewLimitAndResetsQuota)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = false,
            PermitLimit = PermitLimit,
            QueueLimit = QueueLimit,
            Window = FixedWindow
        });

        await AssertReconfigurePersistsNewLimitAndResetsQuotaAsync(
            rateLimiter,
            () => rateLimiter.Configure(new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = false,
                PermitLimit = ExpandedPermitLimit,
                QueueLimit = QueueLimit,
                Window = FixedWindow
            }));
    }

    [Test]
    public async Task SlidingWindowReconfigurePersistsNewLimitAndResetsQuota()
    {
        var rateLimiter = _testApp.Cluster.Client.GetSlidingWindowRateLimiter($"{nameof(SlidingWindowReconfigurePersistsNewLimitAndResetsQuota)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new SlidingWindowRateLimiterOptions
        {
            AutoReplenishment = false,
            PermitLimit = PermitLimit,
            QueueLimit = QueueLimit,
            SegmentsPerWindow = SegmentsPerWindow,
            Window = FixedWindow
        });

        await AssertReconfigurePersistsNewLimitAndResetsQuotaAsync(
            rateLimiter,
            () => rateLimiter.Configure(new SlidingWindowRateLimiterOptions
            {
                AutoReplenishment = false,
                PermitLimit = ExpandedPermitLimit,
                QueueLimit = QueueLimit,
                SegmentsPerWindow = SegmentsPerWindow,
                Window = FixedWindow
            }));
    }

    [Test]
    public async Task TokenBucketReconfigurePersistsNewLimitAndResetsQuota()
    {
        var rateLimiter = _testApp.Cluster.Client.GetTokenBucketRateLimiter($"{nameof(TokenBucketReconfigurePersistsNewLimitAndResetsQuota)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new TokenBucketRateLimiterOptions
        {
            AutoReplenishment = false,
            QueueLimit = QueueLimit,
            ReplenishmentPeriod = FixedWindow,
            TokenLimit = PermitLimit,
            TokensPerPeriod = PermitLimit
        });

        await AssertReconfigurePersistsNewLimitAndResetsQuotaAsync(
            rateLimiter,
            () => rateLimiter.Configure(new TokenBucketRateLimiterOptions
            {
                AutoReplenishment = false,
                QueueLimit = QueueLimit,
                ReplenishmentPeriod = FixedWindow,
                TokenLimit = ExpandedPermitLimit,
                TokensPerPeriod = ExpandedPermitLimit
            }));
    }

    [Test]
    public async Task ConcurrencyReconfigurePersistsNewLimitAndResetsActiveLeases()
    {
        var rateLimiter = _testApp.Cluster.Client.GetConcurrencyLimiter($"{nameof(ConcurrencyReconfigurePersistsNewLimitAndResetsActiveLeases)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new ConcurrencyLimiterOptions
        {
            PermitLimit = PermitLimit,
            QueueLimit = QueueLimit
        });

        var heldLease = await rateLimiter.AcquireAsync();
        heldLease.IsAcquired.ShouldBeTrue();

        await rateLimiter.Configure(new ConcurrencyLimiterOptions
        {
            PermitLimit = ExpandedPermitLimit,
            QueueLimit = QueueLimit
        });
        await ForceActivationCollectionAsync();

        await AssertExactlyExpandedActivePermitLimitAsync(rateLimiter);

        await heldLease.DisposeAsync();
    }

    [Test]
    public async Task FixedWindowAutoReplenishmentRestoresQuotaAfterForcedActivationCollection()
    {
        var rateLimiter = _testApp.Cluster.Client.GetFixedWindowRateLimiter($"{nameof(FixedWindowAutoReplenishmentRestoresQuotaAfterForcedActivationCollection)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = ExpandedPermitLimit,
            QueueLimit = QueueLimit,
            Window = ReplenishmentPeriod
        });

        await AssertAutoReplenishmentRestoresExpandedPermitLimitAsync(rateLimiter);
    }

    [Test]
    public async Task SlidingWindowAutoReplenishmentRestoresQuotaAfterForcedActivationCollection()
    {
        var rateLimiter = _testApp.Cluster.Client.GetSlidingWindowRateLimiter($"{nameof(SlidingWindowAutoReplenishmentRestoresQuotaAfterForcedActivationCollection)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new SlidingWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = ExpandedPermitLimit,
            QueueLimit = QueueLimit,
            SegmentsPerWindow = SegmentsPerWindow,
            Window = ReplenishmentPeriod
        });

        await AssertAutoReplenishmentRestoresExpandedPermitLimitAsync(rateLimiter);
    }

    [Test]
    public async Task TokenBucketAutoReplenishmentRestoresQuotaAfterForcedActivationCollection()
    {
        var rateLimiter = _testApp.Cluster.Client.GetTokenBucketRateLimiter($"{nameof(TokenBucketAutoReplenishmentRestoresQuotaAfterForcedActivationCollection)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new TokenBucketRateLimiterOptions
        {
            AutoReplenishment = true,
            QueueLimit = QueueLimit,
            ReplenishmentPeriod = ReplenishmentPeriod,
            TokenLimit = ExpandedPermitLimit,
            TokensPerPeriod = TokensPerPeriod
        });

        await AssertAutoReplenishmentRestoresExpandedPermitLimitAsync(rateLimiter);
    }

    private static async Task AssertAcquireRejectedAsync(ILimiterHolder rateLimiter)
    {
        await using var rejectedLease = await rateLimiter.AcquireAsync();
        rejectedLease.IsAcquired.ShouldBeFalse();
    }

    private static async Task AssertExactlyExpandedActivePermitLimitAsync(ILimiterHolder rateLimiter)
    {
        var firstLease = await rateLimiter.AcquireAsync();
        var secondLease = await rateLimiter.AcquireAsync();

        try
        {
            firstLease.IsAcquired.ShouldBeTrue();
            secondLease.IsAcquired.ShouldBeTrue();
            await AssertAcquireRejectedAsync(rateLimiter);
        }
        finally
        {
            await secondLease.DisposeAsync();
            await firstLease.DisposeAsync();
        }
    }

    private async Task AssertAutoReplenishmentRestoresExpandedPermitLimitAsync(ILimiterHolder rateLimiter)
    {
        await AssertExactlyExpandedPermitLimitAsync(rateLimiter);
        await ForceActivationCollectionAsync();
        await Task.Delay(ReplenishmentDelay);
        await AssertExactlyExpandedPermitLimitAsync(rateLimiter);
    }

    private static async Task AssertExactlyExpandedPermitLimitAsync(ILimiterHolder rateLimiter)
    {
        await using (var firstLease = await rateLimiter.AcquireAsync())
            firstLease.IsAcquired.ShouldBeTrue();

        await using (var secondLease = await rateLimiter.AcquireAsync())
            secondLease.IsAcquired.ShouldBeTrue();

        await AssertAcquireRejectedAsync(rateLimiter);
    }

    private async Task AssertReconfigurePersistsNewLimitAndResetsQuotaAsync(ILimiterHolder rateLimiter, Func<ValueTask> reconfigure)
    {
        await using (var acquiredBeforeReconfigure = await rateLimiter.AcquireAsync())
            acquiredBeforeReconfigure.IsAcquired.ShouldBeTrue();

        await reconfigure();
        await ForceActivationCollectionAsync();

        await AssertExactlyExpandedPermitLimitAsync(rateLimiter);
    }

    private static async Task AssertReleasedLeaseDoesNotReturnQuotaAsync(ILimiterHolder rateLimiter)
    {
        await using (var acquiredLease = await rateLimiter.AcquireAsync())
            acquiredLease.IsAcquired.ShouldBeTrue();

        await AssertAcquireRejectedAsync(rateLimiter);
    }

    private async Task AssertResetClearsQuotaAsync(ILimiterHolder rateLimiter)
    {
        await using (var acquiredLease = await rateLimiter.AcquireAsync())
            acquiredLease.IsAcquired.ShouldBeTrue();

        await rateLimiter.ResetAsync();
        await ForceActivationCollectionAsync();

        await using var acquiredAfterReset = await rateLimiter.AcquireAsync();
        acquiredAfterReset.IsAcquired.ShouldBeTrue();
    }

    private async Task ForceActivationCollectionAsync()
    {
        var managementGrain = _testApp.Cluster.Client.GetGrain<IManagementGrain>(ManagementGrainKey);
        await managementGrain.ForceActivationCollection(ActivationCollectionAge);
    }
}
