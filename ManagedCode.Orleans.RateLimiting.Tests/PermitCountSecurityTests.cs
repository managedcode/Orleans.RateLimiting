using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;

namespace ManagedCode.Orleans.RateLimiting.Tests;

[ClassDataSource<TestClusterApplication>(Shared = SharedType.PerTestSession)]
public class PermitCountSecurityTests(TestClusterApplication testApp)
{
    private const int PermitLimit = 3;
    private const int RemainingPermits = 0;
    private const int QueueLimit = 0;
    private const int SegmentCount = 2;
    private const int WindowMinutes = 10;

    [Test]
    public async Task NullOptionsPreserveRequestedPermitCountForEveryLimiter()
    {
        var client = testApp.Cluster.Client;
        var concurrency = client.GetConcurrencyLimiter(Guid.NewGuid().ToString());
        await concurrency.Configure(new ConcurrencyLimiterOptions { PermitLimit = PermitLimit, QueueLimit = QueueLimit });
        await VerifyAsync(concurrency);

        var fixedWindow = client.GetFixedWindowRateLimiter(Guid.NewGuid().ToString());
        await fixedWindow.Configure(new FixedWindowRateLimiterOptions
        {
            PermitLimit = PermitLimit,
            QueueLimit = QueueLimit,
            Window = TimeSpan.FromMinutes(WindowMinutes)
        });
        await VerifyAsync(fixedWindow);

        var sliding = client.GetSlidingWindowRateLimiter(Guid.NewGuid().ToString());
        await sliding.Configure(new SlidingWindowRateLimiterOptions
        {
            PermitLimit = PermitLimit,
            QueueLimit = QueueLimit,
            Window = TimeSpan.FromMinutes(WindowMinutes),
            SegmentsPerWindow = SegmentCount
        });
        await VerifyAsync(sliding);

        var token = client.GetTokenBucketRateLimiter(Guid.NewGuid().ToString());
        await token.Configure(new TokenBucketRateLimiterOptions
        {
            TokenLimit = PermitLimit,
            TokensPerPeriod = PermitLimit,
            QueueLimit = QueueLimit,
            ReplenishmentPeriod = TimeSpan.FromMinutes(WindowMinutes)
        });
        await VerifyAsync(token);
    }

    private static async Task VerifyAsync<TOptions>(ILimiterHolderWithConfiguration<TOptions> holder) where TOptions : class
    {
        await using var lease = await holder.AcquireAndCheckConfigurationAsync(PermitLimit, null);
        lease.IsAcquired.ShouldBeTrue();
        (await holder.GetStatisticsAsync())!.CurrentAvailablePermits.ShouldBe(RemainingPermits);
        await using var rejected = await holder.AcquireAsync();
        rejected.IsAcquired.ShouldBeFalse();
    }
}
