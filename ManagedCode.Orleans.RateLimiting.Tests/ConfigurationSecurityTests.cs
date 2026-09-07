using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;

namespace ManagedCode.Orleans.RateLimiting.Tests;

[ClassDataSource<TestClusterApplication>(Shared = SharedType.PerTestSession)]
public class ConfigurationSecurityTests(TestClusterApplication testApp)
{
    private const int PermitLimit = 2;
    private const int InvalidLimit = 0;
    private const int ChangedLimit = 3;
    private const int NegativePermitCount = -1;
    private const int ExcessivePermitCount = 4;
    private const int QueueLimit = 0;
    private const int Segments = 2;
    private const int WindowMinutes = 10;

    [Test]
    [Arguments(Failure.Configure)]
    [Arguments(Failure.AcquireWithInvalidOptions)]
    [Arguments(Failure.AcquireWithNegativeCount)]
    [Arguments(Failure.AcquireWithExcessiveCount)]
    public Task ConcurrencyRetainsQuotaAfterInvalidUpdate(Failure failure) => VerifyAsync(
        testApp.Cluster.Client.GetConcurrencyLimiter(NewKey()),
        new ConcurrencyLimiterOptions { PermitLimit = PermitLimit, QueueLimit = QueueLimit },
        new ConcurrencyLimiterOptions { PermitLimit = InvalidLimit, QueueLimit = QueueLimit },
        new ConcurrencyLimiterOptions { PermitLimit = ChangedLimit, QueueLimit = QueueLimit },
        options => options.PermitLimit, failure, returnsPermits: true);

    [Test]
    [Arguments(Failure.Configure)]
    [Arguments(Failure.AcquireWithInvalidOptions)]
    [Arguments(Failure.AcquireWithNegativeCount)]
    [Arguments(Failure.AcquireWithExcessiveCount)]
    public Task FixedWindowRetainsQuotaAfterInvalidUpdate(Failure failure) => VerifyAsync(
        testApp.Cluster.Client.GetFixedWindowRateLimiter(NewKey()),
        Fixed(PermitLimit), Fixed(InvalidLimit), Fixed(ChangedLimit), options => options.PermitLimit, failure);

    [Test]
    [Arguments(Failure.Configure)]
    [Arguments(Failure.AcquireWithInvalidOptions)]
    [Arguments(Failure.AcquireWithNegativeCount)]
    [Arguments(Failure.AcquireWithExcessiveCount)]
    public Task SlidingWindowRetainsQuotaAfterInvalidUpdate(Failure failure) => VerifyAsync(
        testApp.Cluster.Client.GetSlidingWindowRateLimiter(NewKey()),
        Sliding(PermitLimit), Sliding(InvalidLimit), Sliding(ChangedLimit), options => options.PermitLimit, failure);

    [Test]
    [Arguments(Failure.Configure)]
    [Arguments(Failure.AcquireWithInvalidOptions)]
    [Arguments(Failure.AcquireWithNegativeCount)]
    [Arguments(Failure.AcquireWithExcessiveCount)]
    public Task TokenBucketRetainsQuotaAfterInvalidUpdate(Failure failure) => VerifyAsync(
        testApp.Cluster.Client.GetTokenBucketRateLimiter(NewKey()),
        Token(PermitLimit), Token(InvalidLimit), Token(ChangedLimit), options => options.TokenLimit, failure);

    private static async Task VerifyAsync<TGrain, TOptions>(BaseRateLimiterHolder<TGrain, TOptions> holder,
        TOptions valid, TOptions invalid, TOptions changed, Func<TOptions, int> limit, Failure failure,
        bool returnsPermits = false) where TOptions : class where TGrain : IRateLimiterGrainWithConfiguration<TOptions>
    {
        await holder.Configure(valid);
        await using var active = await holder.AcquireAsync(PermitLimit);
        active.IsAcquired.ShouldBeTrue();

        if (failure == Failure.Configure)
            await Should.ThrowAsync<ArgumentException>(() => holder.Configure(invalid).AsTask());
        else if (failure == Failure.AcquireWithInvalidOptions)
            await Should.ThrowAsync<ArgumentException>(() => holder.AcquireAndCheckConfigurationAsync(invalid));
        else
            await Should.ThrowAsync<ArgumentOutOfRangeException>(() => holder.AcquireAndCheckConfigurationAsync(
                failure == Failure.AcquireWithNegativeCount ? NegativePermitCount : ExcessivePermitCount, changed));

        limit(await holder.GetConfiguration()).ShouldBe(PermitLimit);
        (await holder.GetStatisticsAsync())!.CurrentAvailablePermits.ShouldBe(InvalidLimit);
        await using var rejected = await holder.AcquireAsync();
        rejected.IsAcquired.ShouldBeFalse();
        await active.DisposeAsync();
        if (returnsPermits)
            (await holder.GetStatisticsAsync())!.CurrentAvailablePermits.ShouldBe(PermitLimit);
    }

    private static string NewKey() => Guid.NewGuid().ToString();

    private static FixedWindowRateLimiterOptions Fixed(int limit) => new()
    {
        PermitLimit = limit,
        QueueLimit = QueueLimit,
        Window = TimeSpan.FromMinutes(WindowMinutes)
    };

    private static SlidingWindowRateLimiterOptions Sliding(int limit) => new()
    {
        PermitLimit = limit,
        QueueLimit = QueueLimit,
        Window = TimeSpan.FromMinutes(WindowMinutes),
        SegmentsPerWindow = Segments
    };

    private static TokenBucketRateLimiterOptions Token(int limit) => new()
    {
        TokenLimit = limit,
        TokensPerPeriod = PermitLimit,
        QueueLimit = QueueLimit,
        ReplenishmentPeriod = TimeSpan.FromMinutes(WindowMinutes)
    };

    public enum Failure { Configure, AcquireWithInvalidOptions, AcquireWithNegativeCount, AcquireWithExcessiveCount }
}
