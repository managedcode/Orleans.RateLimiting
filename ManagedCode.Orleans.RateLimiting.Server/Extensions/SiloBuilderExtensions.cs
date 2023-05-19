using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;

namespace ManagedCode.Orleans.RateLimiting.Server.Extensions;

public static class SiloBuilderExtensions
{
    public static IServiceCollection AddOrleansRateLimiting(this ISiloBuilder siloBuilder)
    {
        return siloBuilder.Services;
    }
}