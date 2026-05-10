using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;
using Orleans.Runtime;

namespace ManagedCode.Orleans.RateLimiting.Tests;

[ClassDataSource<TestClusterApplication>(Shared = SharedType.PerTestSession)]
public class RateLimiterPersistenceTests
{
    private const int ManagementGrainKey = 0;
    private const int PermitLimit = 1;
    private const int QueueLimit = 0;
    private const int WindowMinutes = 10;

    private static readonly TimeSpan ActivationCollectionAge = TimeSpan.Zero;
    private static readonly TimeSpan FixedWindow = TimeSpan.FromMinutes(WindowMinutes);

    private readonly TestClusterApplication _testApp;

    public RateLimiterPersistenceTests(TestClusterApplication testApp)
    {
        _testApp = testApp;
    }

    [Test]
    public async Task FixedWindowConsumedQuotaSurvivesForcedActivationCollection()
    {
        var rateLimiter = _testApp.Cluster.Client.GetFixedWindowRateLimiter($"{nameof(FixedWindowConsumedQuotaSurvivesForcedActivationCollection)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = false,
            PermitLimit = PermitLimit,
            QueueLimit = QueueLimit,
            Window = FixedWindow
        });

        await using (var acquiredLease = await rateLimiter.AcquireAsync())
            acquiredLease.IsAcquired.ShouldBeTrue();

        await ForceActivationCollectionAsync();

        await using var rejectedLease = await rateLimiter.AcquireAsync();
        rejectedLease.IsAcquired.ShouldBeFalse();
    }

    [Test]
    public async Task ConcurrencyActiveLeaseSurvivesForcedActivationCollectionUntilReleased()
    {
        var rateLimiter = _testApp.Cluster.Client.GetConcurrencyLimiter($"{nameof(ConcurrencyActiveLeaseSurvivesForcedActivationCollectionUntilReleased)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new ConcurrencyLimiterOptions
        {
            PermitLimit = PermitLimit,
            QueueLimit = QueueLimit
        });

        var heldLease = await rateLimiter.AcquireAsync();
        heldLease.IsAcquired.ShouldBeTrue();

        await ForceActivationCollectionAsync();

        await using (var rejectedLease = await rateLimiter.AcquireAsync())
            rejectedLease.IsAcquired.ShouldBeFalse();

        await heldLease.DisposeAsync();

        await using var acquiredLease = await rateLimiter.AcquireAsync();
        acquiredLease.IsAcquired.ShouldBeTrue();
    }

    [Test]
    public async Task ResetClearsPersistedFixedWindowQuota()
    {
        var rateLimiter = _testApp.Cluster.Client.GetFixedWindowRateLimiter($"{nameof(ResetClearsPersistedFixedWindowQuota)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = false,
            PermitLimit = PermitLimit,
            QueueLimit = QueueLimit,
            Window = FixedWindow
        });

        await using (var acquiredLease = await rateLimiter.AcquireAsync())
            acquiredLease.IsAcquired.ShouldBeTrue();

        await rateLimiter.ResetAsync();
        await ForceActivationCollectionAsync();

        await using var leaseAfterReset = await rateLimiter.AcquireAsync();
        leaseAfterReset.IsAcquired.ShouldBeTrue();
    }

    private async Task ForceActivationCollectionAsync()
    {
        var managementGrain = _testApp.Cluster.Client.GetGrain<IManagementGrain>(ManagementGrainKey);
        await managementGrain.ForceActivationCollection(ActivationCollectionAge);
    }
}
