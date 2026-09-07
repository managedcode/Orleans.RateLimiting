using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;

namespace ManagedCode.Orleans.RateLimiting.Tests;

[ClassDataSource<TimeoutTestCluster>(Shared = SharedType.PerTestSession)]
public class LeaseReleaseOptimizationTests(TimeoutTestCluster app)
{
    private const int PermitCount = 1;
    private const int Segments = 2;
    private const int WindowMinutes = 10;
    private const string Concurrency = "concurrency";
    private const string Fixed = "fixed";
    private const string Sliding = "sliding";
    private const string Token = "token";
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(WindowMinutes);

    [Test]
    [Arguments(Concurrency)]
    [Arguments(Fixed)]
    [Arguments(Sliding)]
    [Arguments(Token)]
    public async Task DisposalUsesOneRpcOnlyWhenPermitsMustBeReturned(string algorithm)
    {
        var grain = await CreateAsync(algorithm);
        var metadata = await grain.AcquireAsync(PermitCount);
        metadata.IsAcquired.ShouldBeTrue();
        metadata.LeaseId.ShouldNotBe(Guid.Empty);
        metadata.IsReleaseOptional.ShouldBe(algorithm != Concurrency);
        var lease = new OrleansRateLimitLease(metadata, app.Cluster.Client);
        app.RpcCounter.Reset();
        await lease.DisposeAsync();
        await lease.DisposeAsync();
        app.RpcCounter.ReleaseCalls.ShouldBe(algorithm == Concurrency ? PermitCount : default);
        (await grain.GetStatisticsAsync())!.CurrentAvailablePermits.ShouldBe(algorithm == Concurrency ? PermitCount : default);
        // A previous client ignores the added flag and still sends ReleaseLease. It remains safe.
        await grain.ReleaseLease(metadata.LeaseId);
        (await grain.GetStatisticsAsync())!.CurrentAvailablePermits.ShouldBe(algorithm == Concurrency ? PermitCount : default);
        await grain.DeleteStateAsync();
    }

    private async Task<IRateLimiterGrain> CreateAsync(string algorithm)
    {
        var key = Guid.NewGuid().ToString();
        switch (algorithm)
        {
            case Concurrency:
                var concurrency = app.Cluster.Client.GetGrain<IConcurrencyLimiterGrain>(key);
                await concurrency.ConfigureAsync(new ConcurrencyLimiterOptions { PermitLimit = PermitCount });
                return concurrency;
            case Fixed:
                var fixedWindow = app.Cluster.Client.GetGrain<IFixedWindowRateLimiterGrain>(key);
                await fixedWindow.ConfigureAsync(new FixedWindowRateLimiterOptions { PermitLimit = PermitCount, Window = Window, AutoReplenishment = false });
                return fixedWindow;
            case Sliding:
                var sliding = app.Cluster.Client.GetGrain<ISlidingWindowRateLimiterGrain>(key);
                await sliding.ConfigureAsync(new SlidingWindowRateLimiterOptions { PermitLimit = PermitCount, Window = Window, SegmentsPerWindow = Segments, AutoReplenishment = false });
                return sliding;
            case Token:
                var token = app.Cluster.Client.GetGrain<ITokenBucketRateLimiterGrain>(key);
                await token.ConfigureAsync(new TokenBucketRateLimiterOptions { TokenLimit = PermitCount, TokensPerPeriod = PermitCount, ReplenishmentPeriod = Window, AutoReplenishment = false });
                return token;
            default: throw new ArgumentOutOfRangeException(nameof(algorithm));
        }
    }
}
