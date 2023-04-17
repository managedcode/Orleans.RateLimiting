using ManagedCode.Orleans.RateLimiting.Server.Extensions;
using Orleans.TestingHost;

namespace ManagedCode.Orleans.RateLimiting.Tests.Cluster;

public class TestSiloConfigurations : ISiloConfigurator
{
    public void Configure(ISiloBuilder siloBuilder)
    {

        siloBuilder.AddOrleansRateLimiting();

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