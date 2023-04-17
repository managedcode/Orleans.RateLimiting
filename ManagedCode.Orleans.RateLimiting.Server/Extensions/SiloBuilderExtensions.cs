using Orleans.Hosting;

namespace ManagedCode.Orleans.RateLimiting.Server.Extensions;

public static class SiloBuilderExtensions
{
    public static ISiloBuilder AddOrleansRateLimiting(this ISiloBuilder siloBuilder)
    {
        return siloBuilder;
    }
}