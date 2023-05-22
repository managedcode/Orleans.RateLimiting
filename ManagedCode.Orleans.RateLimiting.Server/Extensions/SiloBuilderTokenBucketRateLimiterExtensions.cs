using System;
using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Server.GrainCallFilter;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;

namespace ManagedCode.Orleans.RateLimiting.Server.Extensions;

public static class SiloBuilderTokenBucketRateLimiterExtensions
{
    public static ISiloBuilder AddOrleansTokenBucketRateLimiter(this ISiloBuilder siloBuilder, Action<TokenBucketRateLimiterOptions> defaultOptions)
    {
        siloBuilder.Services.AddOptions<TokenBucketRateLimiterOptions>().Configure(defaultOptions.Invoke);
        siloBuilder.AddIncomingGrainCallFilter<TokenBucketRateLimiterIncomingFilter>();
        return siloBuilder;
    }
}