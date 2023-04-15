using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Extensions;

public static class GrainFactoryExtensions
{
    public static FixedWindowRateLimiterHolder GetFixedWindowRateLimiter(this IGrainFactory factory, string key)
    {
        return new FixedWindowRateLimiterHolder(factory.GetGrain<IFixedWindowRateLimiterGrain>(key), factory);
    }

    public static ConcurrencyLimiterHolder GetConcurrencyLimiter(this IGrainFactory factory, string key)
    {
        return new ConcurrencyLimiterHolder(factory.GetGrain<IConcurrencyLimiterGrain>(key), factory);
    }

    public static SlidingWindowRateLimiterHolder GetSlidingWindowRateLimiter(this IGrainFactory factory, string key)
    {
        return new SlidingWindowRateLimiterHolder(factory.GetGrain<ISlidingWindowRateLimiterGrain>(key), factory);
    }

    public static TokenBucketRateLimiterHolder GetTokenBucketRateLimiter(this IGrainFactory factory, string key)
    {
        return new TokenBucketRateLimiterHolder(factory.GetGrain<ITokenBucketRateLimiterGrain>(key), factory);
    }
}