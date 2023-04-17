using System;
using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Server.GrainCallFilter;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;

namespace ManagedCode.Orleans.RateLimiting.Server.Extensions;

public static class SiloBuilderTokenBucketRateLimiterExtensions
{
    public static ISiloBuilder AddOrleansTokenBucketRateLimiter(this ISiloBuilder siloBuilder,
        TokenBucketRateLimiterOptions defaultOptions)
    {
        siloBuilder.Services.AddSingleton(defaultOptions);
        siloBuilder.AddIncomingGrainCallFilter<TokenBucketRateLimiterIncomingFilter>();
        return siloBuilder;
    }

    public static ISiloBuilder AddOrleansTokenBucketRateLimiter(this ISiloBuilder siloBuilder,
        Action<TokenBucketRateLimiterOptions> defaultOptions)
    {
        var fixedWindowRateLimiterOptions = new TokenBucketRateLimiterOptions();
        defaultOptions.Invoke(fixedWindowRateLimiterOptions);
        return siloBuilder.AddOrleansTokenBucketRateLimiter(fixedWindowRateLimiterOptions);
    }
}