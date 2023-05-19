using System;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using Orleans.Runtime;

namespace ManagedCode.Orleans.RateLimiting.Client.Extensions;

public static class ClientBuilderExtensions
{
    public static IClientBuilder AddOrleansRateLimiting(this IClientBuilder clientBuilder)
    {
        return clientBuilder;
    }
}