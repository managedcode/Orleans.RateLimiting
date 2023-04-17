using System;
using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Server.GrainCallFilter;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;

namespace ManagedCode.Orleans.RateLimiting.Server.Extensions;

public static class SiloBuilderFixedWindowRateLimiterExtensions
{
    public static ISiloBuilder AddOrleansFixedWindowRateLimiter(this ISiloBuilder siloBuilder,
        Action<FixedWindowRateLimiterOptions> defaultOptions)
    {
        siloBuilder.Services.AddOptions<FixedWindowRateLimiterOptions>().Configure(defaultOptions.Invoke);
        siloBuilder.AddIncomingGrainCallFilter<FixedWindowRateLimiterIncomingFilter>();
        return siloBuilder;
    }
}