using ManagedCode.Orleans.RateLimiting.Core.Attributes;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster.Grains.Interfaces;

namespace ManagedCode.Orleans.RateLimiting.Tests.Cluster.Grains;

public class TestTokenBucketRateLimiterGrain : Grain, ITestTokenBucketRateLimiterGrain
{
    [TokenBucketRateLimiter]  //GrainId as key, default options
    public async Task<string> Do()
    {
        await Task.Delay(TimeSpan.FromSeconds(5));
        return "Do";
    }

    [TokenBucketRateLimiter(KeyType.Key, "go")] //String as Key, default options
    public async Task<string> Go()
    {
        await Task.Delay(TimeSpan.FromSeconds(5));
        return "Go";
    }

    [TokenBucketRateLimiter(KeyType.GrainType, tokenLimit:2, queueLimit:1)] //GrainType as Key, custom options, some of them are default (check Attribute)
    public async Task<string> Take()
    {
        await Task.Delay(TimeSpan.FromSeconds(5));
        return "Take";
    }
}