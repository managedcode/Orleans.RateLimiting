using ManagedCode.Orleans.RateLimiting.Client.Extensions;
using Microsoft.Extensions.Configuration;
using Orleans.TestingHost;

namespace ManagedCode.Orleans.RateLimiting.Tests.Cluster;

public class TestClientConfigurations : IClientBuilderConfigurator
{
    public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
    {
        clientBuilder.AddOrleansRateLimiting();
    }
}