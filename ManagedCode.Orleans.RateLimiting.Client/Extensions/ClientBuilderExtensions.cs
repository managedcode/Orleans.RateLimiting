using Orleans;
using Orleans.Hosting;

namespace ManagedCode.Orleans.RateLimiting.Client.Extensions;

public static class ClientBuilderExtensions
{
    public static IClientBuilder AddOrleansRateLimiting(this IClientBuilder clientBuilder)
    {
        clientBuilder.Services.AddOrleansRateLimiting();
        return clientBuilder;
    }
}
