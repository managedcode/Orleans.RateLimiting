using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Server.Extensions;
using Orleans.TestingHost;

namespace ManagedCode.Orleans.RateLimiting.Tests.Cluster;

public class TestSiloConfigurations : ISiloConfigurator
{
    public void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder.AddOrleansRateLimiting();

        siloBuilder.Services.AddOrleansRateLimiterOptions("ConcurrencyLimiterOptions1", new ConcurrencyLimiterOptions
        {
            PermitLimit = 10,
            QueueLimit = 15
        });

        siloBuilder.Services.AddOrleansConcurrencyLimiterOptions("ConcurrencyLimiterOptions2", options =>
        {
            options.PermitLimit = 2;
            options.QueueLimit = 2;
        });


        siloBuilder.Services.AddOrleansRateLimiterOptions("FixedWindowRateLimiterOptions1", new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            QueueLimit = 15,
            Window = TimeSpan.FromSeconds(1)
        });

        siloBuilder.Services.AddFixedWindowRateLimiterOptions("FixedWindowRateLimiterOptions2", options =>
        {
            options.PermitLimit = 2;
            options.QueueLimit = 2;
            options.Window = TimeSpan.FromSeconds(2);
        });


        siloBuilder.Services.AddOrleansRateLimiterOptions("SlidingWindowRateLimiterOptions1", new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 10,
            QueueLimit = 15,
            Window = TimeSpan.FromSeconds(1)
        });

        siloBuilder.Services.AddSlidingWindowRateLimiterOptions("SlidingWindowRateLimiterOptions2", options =>
        {
            options.PermitLimit = 10;
            options.QueueLimit = 15;
            options.Window = TimeSpan.FromSeconds(2);
        });


        siloBuilder.Services.AddOrleansRateLimiterOptions("TokenBucketRateLimiterOptions1", new TokenBucketRateLimiterOptions
        {
            QueueLimit = 10,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            TokenLimit = 15,
            TokensPerPeriod = 2
        });
        siloBuilder.Services.AddTokenBucketRateLimiterOptions("TokenBucketRateLimiterOptions2", options =>
        {
            options.QueueLimit = 1;
            options.ReplenishmentPeriod = TimeSpan.FromSeconds(2);
            options.TokenLimit = 1;
            options.TokensPerPeriod = 1;
        });


        //Add default options and IncomingFilter
        siloBuilder.AddOrleansConcurrencyLimiter(options =>
        {
            options.PermitLimit = 10;
            options.QueueLimit = 15;
        });

        //Add default options and IncomingFilter
        siloBuilder.AddOrleansFixedWindowRateLimiter(options =>
        {
            options.PermitLimit = 10;
            options.QueueLimit = 15;
            options.Window = TimeSpan.FromSeconds(1);
        });

        //Add default options and IncomingFilter
        siloBuilder.AddOrleansSlidingWindowRateLimiter(options =>
        {
            options.PermitLimit = 10;
            options.QueueLimit = 15;
            options.Window = TimeSpan.FromSeconds(1);
            options.SegmentsPerWindow = 2;
        });

        //Add default options and IncomingFilter
        siloBuilder.AddOrleansTokenBucketRateLimiter(options =>
        {
            options.TokenLimit = 10;
            options.QueueLimit = 15;
            options.TokensPerPeriod = 2;
            options.ReplenishmentPeriod = TimeSpan.FromSeconds(1);
        });
    }
}