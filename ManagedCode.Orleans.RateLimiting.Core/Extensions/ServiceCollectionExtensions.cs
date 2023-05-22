using System;
using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Orleans.RateLimiting.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrleansRateLimiterOptions(this IServiceCollection collection, string name, ConcurrencyLimiterOptions options)
    {
        collection.AddSingleton(new RateLimiterConfig(name, options));
        return collection;
    }

    public static IServiceCollection AddOrleansRateLimiterOptions(this IServiceCollection collection, string name, FixedWindowRateLimiterOptions options)
    {
        collection.AddSingleton(new RateLimiterConfig(name, options));
        return collection;
    }

    public static IServiceCollection AddOrleansRateLimiterOptions(this IServiceCollection collection, string name, SlidingWindowRateLimiterOptions options)
    {
        collection.AddSingleton(new RateLimiterConfig(name, options));
        return collection;
    }

    public static IServiceCollection AddOrleansRateLimiterOptions(this IServiceCollection collection, string name, TokenBucketRateLimiterOptions options)
    {
        collection.AddSingleton(new RateLimiterConfig(name, options));
        return collection;
    }


    public static IServiceCollection AddOrleansConcurrencyLimiterOptions(this IServiceCollection collection, string name, Action<ConcurrencyLimiterOptions> options)
    {
        var option = new ConcurrencyLimiterOptions();
        options.Invoke(option);
        return collection.AddOrleansRateLimiterOptions(name, option);
    }

    public static IServiceCollection AddFixedWindowRateLimiterOptions(this IServiceCollection collection, string name, Action<FixedWindowRateLimiterOptions> options)
    {
        var option = new FixedWindowRateLimiterOptions();
        options.Invoke(option);
        return collection.AddOrleansRateLimiterOptions(name, option);
    }

    public static IServiceCollection AddSlidingWindowRateLimiterOptions(this IServiceCollection collection, string name, Action<SlidingWindowRateLimiterOptions> options)
    {
        var option = new SlidingWindowRateLimiterOptions();
        options.Invoke(option);
        return collection.AddOrleansRateLimiterOptions(name, option);
    }

    public static IServiceCollection AddTokenBucketRateLimiterOptions(this IServiceCollection collection, string name, Action<TokenBucketRateLimiterOptions> options)
    {
        var option = new TokenBucketRateLimiterOptions();
        options.Invoke(option);
        return collection.AddOrleansRateLimiterOptions(name, option);
    }
}