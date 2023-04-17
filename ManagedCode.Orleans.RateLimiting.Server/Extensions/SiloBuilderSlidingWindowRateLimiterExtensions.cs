using System;
using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Server.GrainCallFilter;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;

namespace ManagedCode.Orleans.RateLimiting.Server.Extensions;

public static class SiloBuilderSlidingWindowRateLimiterExtensions
{
    public static ISiloBuilder AddOrleansSlidingWindowRateLimiter(this ISiloBuilder siloBuilder,
        SlidingWindowRateLimiterOptions defaultOptions)
    {
        siloBuilder.Services.AddSingleton(defaultOptions);
        siloBuilder.AddIncomingGrainCallFilter<SlidingWindowRateLimiterIncomingFilter>();
        return siloBuilder;
    }

    public static ISiloBuilder AddOrleansSlidingWindowRateLimiter(this ISiloBuilder siloBuilder,
        Action<SlidingWindowRateLimiterOptions> defaultOptions)
    {
        var fixedWindowRateLimiterOptions = new SlidingWindowRateLimiterOptions();
        defaultOptions.Invoke(fixedWindowRateLimiterOptions);
        return siloBuilder.AddOrleansSlidingWindowRateLimiter(fixedWindowRateLimiterOptions);
    }
}