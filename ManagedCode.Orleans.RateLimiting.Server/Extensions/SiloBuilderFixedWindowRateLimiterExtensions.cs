using System;
using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Server.GrainCallFilter;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;

namespace ManagedCode.Orleans.RateLimiting.Server.Extensions;

public static class SiloBuilderFixedWindowRateLimiterExtensions
{
    public static ISiloBuilder AddOrleansFixedWindowRateLimiter(this ISiloBuilder siloBuilder,
        FixedWindowRateLimiterOptions defaultOptions)
    {
        siloBuilder.Services.AddSingleton(defaultOptions);
        siloBuilder.AddIncomingGrainCallFilter<FixedWindowRateLimiterIncomingFilter>();
        return siloBuilder;
    }

    public static ISiloBuilder AddOrleansFixedWindowRateLimiter(this ISiloBuilder siloBuilder,
        Action<FixedWindowRateLimiterOptions> defaultOptions)
    {
        var fixedWindowRateLimiterOptions = new FixedWindowRateLimiterOptions();
        defaultOptions.Invoke(fixedWindowRateLimiterOptions);
        return siloBuilder.AddOrleansFixedWindowRateLimiter(fixedWindowRateLimiterOptions);
    }
}