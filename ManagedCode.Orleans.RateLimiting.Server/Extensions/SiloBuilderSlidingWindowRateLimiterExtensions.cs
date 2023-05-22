using System;
using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Server.GrainCallFilter;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;

namespace ManagedCode.Orleans.RateLimiting.Server.Extensions;

public static class SiloBuilderSlidingWindowRateLimiterExtensions
{
    public static ISiloBuilder AddOrleansSlidingWindowRateLimiter(this ISiloBuilder siloBuilder, Action<SlidingWindowRateLimiterOptions> defaultOptions)
    {
        siloBuilder.Services.AddOptions<SlidingWindowRateLimiterOptions>().Configure(defaultOptions.Invoke);
        siloBuilder.AddIncomingGrainCallFilter<SlidingWindowRateLimiterIncomingFilter>();
        return siloBuilder;
    }
}