using System.Collections.Generic;
using System.Linq;
using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Extensions;

public static class GrainFactoryExtensions
{
    public static ILimiterHolder GetRateLimiter<T>(this IGrainFactory factory, string key) where T : IRateLimiterGrain
    {
        ILimiterHolder limiter = typeof(T) switch
        {
            IFixedWindowRateLimiterGrain => factory.GetFixedWindowRateLimiter(key),
            IConcurrencyLimiterGrain => factory.GetConcurrencyLimiter(key),
            ISlidingWindowRateLimiterGrain => factory.GetSlidingWindowRateLimiter(key),
            ITokenBucketRateLimiterGrain => factory.GetTokenBucketRateLimiter(key),

            _ => null //throw new ArgumentException("Unknown rate limiter grain type")
        };

        return limiter;
    }

    public static ILimiterHolder? GetRateLimiterByConfig(this IGrainFactory factory, string key, string configurationName, IEnumerable<RateLimiterConfig> configs)
    {
        var name = configurationName.ToLowerInvariant();
        var option = configs.FirstOrDefault(f => f.Name == name);
        if (option is null)
            return null;

        ILimiterHolder? limiter = option.Configuration switch
        {
            FixedWindowRateLimiterOptions options => factory.GetFixedWindowRateLimiter(key, options),
            ConcurrencyLimiterOptions options => factory.GetConcurrencyLimiter(key, options),
            SlidingWindowRateLimiterOptions options => factory.GetSlidingWindowRateLimiter(key, options),
            TokenBucketRateLimiterOptions options => factory.GetTokenBucketRateLimiter(key, options),

            _ => null //throw new ArgumentException("Unknown rate limiter grain type")
        };

        return limiter;
    }

    public static FixedWindowRateLimiterHolder GetFixedWindowRateLimiter(this IGrainFactory factory, string key)
    {
        return new FixedWindowRateLimiterHolder(factory.GetGrain<IFixedWindowRateLimiterGrain>(key), factory);
    }

    public static FixedWindowRateLimiterHolder GetFixedWindowRateLimiter(this IGrainFactory factory, string key, FixedWindowRateLimiterOptions options)
    {
        return new FixedWindowRateLimiterHolder(factory.GetGrain<IFixedWindowRateLimiterGrain>(key), factory, options);
    }

    public static ConcurrencyLimiterHolder GetConcurrencyLimiter(this IGrainFactory factory, string key)
    {
        return new ConcurrencyLimiterHolder(factory.GetGrain<IConcurrencyLimiterGrain>(key), factory);
    }

    public static ConcurrencyLimiterHolder GetConcurrencyLimiter(this IGrainFactory factory, string key, ConcurrencyLimiterOptions options)
    {
        return new ConcurrencyLimiterHolder(factory.GetGrain<IConcurrencyLimiterGrain>(key), factory, options);
    }

    public static SlidingWindowRateLimiterHolder GetSlidingWindowRateLimiter(this IGrainFactory factory, string key)
    {
        return new SlidingWindowRateLimiterHolder(factory.GetGrain<ISlidingWindowRateLimiterGrain>(key), factory);
    }

    public static SlidingWindowRateLimiterHolder GetSlidingWindowRateLimiter(this IGrainFactory factory, string key, SlidingWindowRateLimiterOptions options)
    {
        return new SlidingWindowRateLimiterHolder(factory.GetGrain<ISlidingWindowRateLimiterGrain>(key), factory, options);
    }

    public static TokenBucketRateLimiterHolder GetTokenBucketRateLimiter(this IGrainFactory factory, string key)
    {
        return new TokenBucketRateLimiterHolder(factory.GetGrain<ITokenBucketRateLimiterGrain>(key), factory);
    }

    public static TokenBucketRateLimiterHolder GetTokenBucketRateLimiter(this IGrainFactory factory, string key, TokenBucketRateLimiterOptions options)
    {
        return new TokenBucketRateLimiterHolder(factory.GetGrain<ITokenBucketRateLimiterGrain>(key), factory, options);
    }
}