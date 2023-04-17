using ManagedCode.Orleans.RateLimiting.Tests.Cluster.Grains.Interfaces;

namespace ManagedCode.Orleans.RateLimiting.Tests.Cluster.Grains;

public class ThirdLimitingGrain : Grain, IThirdLimitingGrain
{
    public Task<string> Do()
    {
        return Task.FromResult("Do");
    }

    public Task<string> Go()
    {
        return Task.FromResult("Go");
    }

    public Task<string> Take()
    {
        return Task.FromResult("Take");
    }
}