using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orleans;
using Orleans.Hosting;

namespace ManagedCode.Orleans.RateLimiting.Client.Extensions;

public static class ClientBuilderExtensions
{
    public static IClientBuilder AddOrleansRateLimiting(this IClientBuilder clientBuilder)
    {
        clientBuilder.Services.AddOrleansRateLimitingCore();
        clientBuilder.Services.TryAddSingleton<IGrainFactory>(serviceProvider => serviceProvider.GetRequiredService<IClusterClient>());
        return clientBuilder;
    }
}
