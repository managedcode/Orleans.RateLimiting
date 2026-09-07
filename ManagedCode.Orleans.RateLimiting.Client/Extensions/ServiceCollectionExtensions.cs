using System.Linq;
using ManagedCode.Orleans.RateLimiting.Core.Services;
using ManagedCode.Orleans.RateLimiting.Client.GrainCallFilters;
using System;
using ManagedCode.Orleans.RateLimiting.Core.Options;
using ManagedCode.Orleans.RateLimiting.Client.Middlewares;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Client.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrleansRateLimiting(this IServiceCollection collection)
    {
        ManagedCode.Orleans.RateLimiting.Core.Extensions.ServiceCollectionExtensions.AddOrleansRateLimitingCore(collection);
        if (!collection.Any(descriptor => descriptor.ServiceType == typeof(IOutgoingGrainCallFilter)
            && descriptor.ImplementationType is { } implementation && typeof(RateLimiterTimeoutFilter).IsAssignableFrom(implementation)))
            collection.AddSingleton<IOutgoingGrainCallFilter, ClientRateLimiterTimeoutFilter>();
        collection.TryAddSingleton<IGrainFactory>(sp => sp.GetRequiredService<IClusterClient>());
        return collection;
    }

    public static IServiceCollection AddOrleansRateLimiting(this IServiceCollection collection, Action<RateLimitRequestOrchestrationOptions> configure)
    {
        collection.AddOrleansRateLimiting();
        collection.Configure(configure);
        return collection;
    }
}
