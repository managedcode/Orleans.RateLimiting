using Microsoft.Extensions.DependencyInjection;
using ManagedCode.Orleans.RateLimiting.Client.Extensions;
using Orleans.Configuration;
using Orleans.TestingHost;
using TUnit.Core.Interfaces;

namespace ManagedCode.Orleans.RateLimiting.Tests.Cluster;

public sealed class TimeoutTestCluster : IAsyncInitializer, IAsyncDisposable
{
    private const short SiloCount = 1;
    private const int TimeoutSeconds = 1;
    private const int SiloTimeoutMilliseconds = 500;
    public static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(TimeoutSeconds);
    public LeaseRpcCounter RpcCounter { get; } = new();
    public InProcessTestCluster Cluster { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var builder = new InProcessTestClusterBuilder(SiloCount);
        builder.ConfigureSilo((_, silo) =>
        {
            new TestSiloConfigurations().Configure(silo);
            silo.Services.Configure<SiloMessagingOptions>(options =>
            {
                options.ResponseTimeout = TimeSpan.FromMilliseconds(SiloTimeoutMilliseconds);
                options.CancelRequestOnTimeout = false;
            });
        });
        builder.ConfigureClient(client => client.AddOrleansRateLimiting().Services.Configure<ClientMessagingOptions>(options =>
        {
            options.ResponseTimeout = ResponseTimeout;
            options.CancelRequestOnTimeout = false;
        }));
        builder.ConfigureClient(client => client.Services.AddSingleton<IOutgoingGrainCallFilter>(RpcCounter));
        Cluster = builder.Build();
        await Cluster.DeployAsync();
    }

    public ValueTask DisposeAsync() => Cluster.DisposeAsync();
}
