using System.Linq;
using ManagedCode.Orleans.RateLimiting.Core.Services;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Server.Options;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using Orleans;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ManagedCode.Orleans.RateLimiting.Server.GrainCallFilter;

namespace ManagedCode.Orleans.RateLimiting.Server.Extensions;

public static class SiloBuilderExtensions
{
    public static IServiceCollection AddOrleansRateLimiting(this ISiloBuilder siloBuilder)
    {
        siloBuilder.Services.AddOrleansRateLimitingCore();
        // A co-hosted client uses the silo runtime. Supply its response timeout once per call.
        foreach (var descriptor in siloBuilder.Services.Where(descriptor => descriptor.ServiceType == typeof(IOutgoingGrainCallFilter)
            && descriptor.ImplementationType is { } implementation && typeof(RateLimiterTimeoutFilter).IsAssignableFrom(implementation)).ToArray())
            siloBuilder.Services.Remove(descriptor);
        siloBuilder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IOutgoingGrainCallFilter, SiloRateLimiterTimeoutFilter>());
        siloBuilder.Services.AddOptions<RateLimiterPersistenceOptions>();
        return siloBuilder.Services;
    }
}
