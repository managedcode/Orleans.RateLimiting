using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;

namespace ManagedCode.Orleans.RateLimiting.Server.Extensions;

public static class SiloBuilderExtensions
{
    public static IServiceCollection AddOrleansRateLimiting(this ISiloBuilder siloBuilder)
    {
        siloBuilder.Services.AddOrleansRateLimitingCore();
        return siloBuilder.Services;
    }
}
