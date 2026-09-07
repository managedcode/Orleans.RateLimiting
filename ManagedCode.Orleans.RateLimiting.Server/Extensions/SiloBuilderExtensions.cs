using ManagedCode.Orleans.RateLimiting.Core.Services;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Server.Options;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ManagedCode.Orleans.RateLimiting.Server.Services;

namespace ManagedCode.Orleans.RateLimiting.Server.Extensions;

public static class SiloBuilderExtensions
{
    public static IServiceCollection AddOrleansRateLimiting(this ISiloBuilder siloBuilder)
    {
        siloBuilder.Services.AddOrleansRateLimitingCore();
        // A co-hosted client uses the silo runtime. Supply its response timeout once per call.
        siloBuilder.Services.RemoveAll<RateLimiterTimeoutProvider>();
        siloBuilder.Services.AddSingleton<RateLimiterTimeoutProvider, SiloRateLimiterTimeoutProvider>();
        siloBuilder.Services.AddOptions<RateLimiterPersistenceOptions>();
        return siloBuilder.Services;
    }
}
