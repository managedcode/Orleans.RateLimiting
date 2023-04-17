using System;
using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Server.GrainCallFilter;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;

namespace ManagedCode.Orleans.RateLimiting.Server.Extensions;

public static class SiloBuilderConcurrencyLimiterExtensions
{
    public static ISiloBuilder AddOrleansConcurrencyLimiter(this ISiloBuilder siloBuilder,
        ConcurrencyLimiterOptions defaultOptions)
    {
        siloBuilder.Services.AddSingleton(defaultOptions);
        siloBuilder.AddIncomingGrainCallFilter<ConcurrencyLimiterIncomingFilter>();
        return siloBuilder;
    }

    public static ISiloBuilder AddOrleansConcurrencyLimiter(this ISiloBuilder siloBuilder,
        Action<ConcurrencyLimiterOptions> defaultOptions)
    {
        var fixedWindowRateLimiterOptions = new ConcurrencyLimiterOptions();
        defaultOptions.Invoke(fixedWindowRateLimiterOptions);
        return siloBuilder.AddOrleansConcurrencyLimiter(fixedWindowRateLimiterOptions);
    }
}