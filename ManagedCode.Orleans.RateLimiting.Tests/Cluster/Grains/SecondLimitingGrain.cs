using ManagedCode.Orleans.RateLimiting.Tests.Cluster.Grains.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace ManagedCode.Orleans.RateLimiting.Tests.Cluster.Grains;


public class SecondLimitingGrain : Grain, ISecondLimitingGrain
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