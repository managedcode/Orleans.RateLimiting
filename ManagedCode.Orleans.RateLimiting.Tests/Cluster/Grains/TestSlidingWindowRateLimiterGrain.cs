using ManagedCode.Orleans.RateLimiting.Core.Attributes;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster.Grains.Interfaces;

namespace ManagedCode.Orleans.RateLimiting.Tests.Cluster.Grains;

public class TestSlidingWindowRateLimiterGrain : Grain, ITestSlidingWindowRateLimiterGrain
{
    [SlidingWindowRateLimiter]  //GrainId as key, default options
    public async Task<string> Do()
    {
        await Task.Delay(TimeSpan.FromSeconds(5));
        return "Do";
    }

    [SlidingWindowRateLimiter(KeyType.Key, "go")] //String as Key, default options
    public async Task<string> Go()
    {
        await Task.Delay(TimeSpan.FromSeconds(5));
        return "Go";
    }

    [SlidingWindowRateLimiter(KeyType.GrainType, permitLimit:2, queueLimit:1, segmentsPerWindow:2)] //GrainType as Key, custom options, some of them are default (check Attribute)
    public async Task<string> Take()
    {
        await Task.Delay(TimeSpan.FromSeconds(5));
        return "Take";
    }
    
    [SlidingWindowRateLimiter("SlidingWindowRateLimiterOptions1", KeyType.Key, "Skip")] //String as Key, default options
    public async Task<string> Skip()
    {
        await Task.Delay(TimeSpan.FromSeconds(5));
        return "Skip";
    }
}