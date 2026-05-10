using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Server.Options;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;

namespace ManagedCode.Orleans.RateLimiting.Server.Extensions;

public static class SiloBuilderExtensions
{
    public static IServiceCollection AddOrleansRateLimiting(this ISiloBuilder siloBuilder)
    {
        siloBuilder.Services.AddOrleansRateLimitingCore();
        siloBuilder.Services.AddOptions<RateLimiterPersistenceOptions>();
        return siloBuilder.Services;
    }
}
