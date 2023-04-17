using System;
using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Server.GrainCallFilter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Hosting;

namespace ManagedCode.Orleans.RateLimiting.Server.Extensions;

public static class SiloBuilderConcurrencyLimiterExtensions
{
    public static ISiloBuilder AddOrleansConcurrencyLimiter(this ISiloBuilder siloBuilder,
        Action<ConcurrencyLimiterOptions> defaultOptions)
    {
        siloBuilder.Services.AddOptions<ConcurrencyLimiterOptions>().Configure(defaultOptions.Invoke);
        siloBuilder.AddIncomingGrainCallFilter<ConcurrencyLimiterIncomingFilter>();
        return siloBuilder;
    }
}