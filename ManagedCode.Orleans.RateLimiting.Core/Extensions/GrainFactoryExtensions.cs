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
        return typeof(T) switch
        {
            var type when type == typeof(IFixedWindowRateLimiterGrain) => factory.GetFixedWindowRateLimiter(key),
            var type when type == typeof(IConcurrencyLimiterGrain) => factory.GetConcurrencyLimiter(key),
            var type when type == typeof(ISlidingWindowRateLimiterGrain) => factory.GetSlidingWindowRateLimiter(key),
            var type when type == typeof(ITokenBucketRateLimiterGrain) => factory.GetTokenBucketRateLimiter(key),

            _ => throw new System.NotSupportedException($"Rate limiter grain type '{typeof(T).FullName}' is not supported.")
        };
    }

    public static ILimiterHolder? GetRateLimiterByConfig(this IGrainFactory factory, string key, string configurationName, IEnumerable<RateLimiterConfig> configs)
    {
        var option = configs.FirstOrDefault(f => f.NameEquals(configurationName));
        if (option is null)
            return null;

        return option.Configuration switch
        {
            FixedWindowRateLimiterOptions options => factory.GetFixedWindowRateLimiter(key, options),
            ConcurrencyLimiterOptions options => factory.GetConcurrencyLimiter(key, options),
            SlidingWindowRateLimiterOptions options => factory.GetSlidingWindowRateLimiter(key, options),
            TokenBucketRateLimiterOptions options => factory.GetTokenBucketRateLimiter(key, options),

            _ => null
        };
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
