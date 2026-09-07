using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;

namespace ManagedCode.Orleans.RateLimiting.Tests;

[ClassDataSource<TimeoutTestCluster>(Shared = SharedType.PerTestSession)]
public class LegacyLeaseRpcCompatibilityTests(TimeoutTestCluster app)
{
    private const int PermitLimit = 2;
    private const int PermitCount = 1;
    private const int QueueLimit = 0;
    private const int Segments = 2;
    private const int WindowMinutes = 10;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(WindowMinutes);

    [Test]
    public Task LegacyConcurrencyCallsPreserveConfigurationAndLeaseOwnership()
        => VerifyAsync<IConcurrencyLimiterGrain, ConcurrencyLimiterOptions>(new()
        { PermitLimit = PermitLimit, QueueLimit = QueueLimit }, returnsPermits: true);

    [Test]
    public Task LegacyFixedWindowCallsPreserveConfigurationAndQuota()
        => VerifyAsync<IFixedWindowRateLimiterGrain, FixedWindowRateLimiterOptions>(new()
        { PermitLimit = PermitLimit, QueueLimit = QueueLimit, Window = Window, AutoReplenishment = false }, returnsPermits: false);

    [Test]
    public Task LegacySlidingWindowCallsPreserveConfigurationAndQuota()
        => VerifyAsync<ISlidingWindowRateLimiterGrain, SlidingWindowRateLimiterOptions>(new()
        { PermitLimit = PermitLimit, QueueLimit = QueueLimit, Window = Window, SegmentsPerWindow = Segments, AutoReplenishment = false }, returnsPermits: false);

    [Test]
    public Task LegacyTokenBucketCallsPreserveConfigurationAndQuota()
        => VerifyAsync<ITokenBucketRateLimiterGrain, TokenBucketRateLimiterOptions>(new()
        { TokenLimit = PermitLimit, TokensPerPeriod = PermitLimit, QueueLimit = QueueLimit, ReplenishmentPeriod = Window, AutoReplenishment = false }, returnsPermits: false);

    private async Task VerifyAsync<TGrain, TOptions>(TOptions options, bool returnsPermits)
        where TGrain : IRateLimiterGrainWithConfiguration<TOptions>
        where TOptions : class
    {
        var grain = app.Cluster.Client.GetGrain<TGrain>(Guid.NewGuid().ToString());
        var first = await grain.AcquireAndCheckConfigurationAsync(options);
        var second = await grain.AcquireAndCheckConfigurationAsync(PermitCount, options);
        first.IsAcquired.ShouldBeTrue();
        second.IsAcquired.ShouldBeTrue();
        first.LeaseId.ShouldNotBe(second.LeaseId);
        (await grain.AcquireAndCheckConfigurationAsync(PermitCount, options)).IsAcquired.ShouldBeFalse();
        await grain.ReleaseLease(first.LeaseId);
        await grain.ReleaseLease(second.LeaseId);
        (await grain.GetStatisticsAsync())!.CurrentAvailablePermits.ShouldBe(returnsPermits ? PermitLimit : QueueLimit);
        await grain.DeleteStateAsync();
    }
}
