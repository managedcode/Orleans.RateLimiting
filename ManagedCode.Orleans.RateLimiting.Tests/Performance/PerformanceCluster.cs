using Orleans.Messaging;
using ManagedCode.Orleans.RateLimiting.Client.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Configuration;
using Orleans.Runtime;
using Orleans.Runtime.Placement;
using Orleans.TestingHost;
using TUnit.Core.Interfaces;

namespace ManagedCode.Orleans.RateLimiting.Tests.Performance;

public sealed class PerformanceCluster : IAsyncInitializer, IAsyncDisposable
{
    private const short SiloCount = 2;
    private const int GatewayIndex = 0;
    private const int GatewayCount = 1;
    private const int PlacementIndex = 0;
    public InProcessTestCluster Cluster { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var builder = new InProcessTestClusterBuilder(SiloCount);
        builder.Options.GatewayPerSilo = false;
        builder.ConfigureSilo((_, silo) => new TestSiloConfigurations().Configure(silo));
        builder.ConfigureClient(client => client.AddOrleansRateLimiting().Services.Configure<GatewayOptions>(options => options.PreferredGatewayIndex = GatewayIndex));
        Cluster = builder.Build();
        await Cluster.DeployAsync();
        var gateways = await Cluster.Client.ServiceProvider.GetRequiredService<IGatewayListProvider>().GetGateways();
        gateways.Count.ShouldBe(GatewayCount);
        var expected = Cluster.Silos[PlacementIndex].SiloHost.Services.GetRequiredService<ILocalSiloDetails>().GatewayAddress!;
        gateways.Single().Host.ShouldBe(expected.Endpoint.Address.ToString());
        gateways.Single().Port.ShouldBe(expected.Endpoint.Port);
    }

    internal async Task PlaceAsync(ICancellableLimiterHolder holder, GrainId grainId)
    {
        var target = Cluster.Silos[PlacementIndex].SiloAddress;
        RequestContext.Set(IPlacementDirector.PlacementHintKey, target);
        try { await PerformanceMeasurement.AcquireReleaseAsync(holder, cancellable: false); }
        finally { RequestContext.Remove(IPlacementDirector.PlacementHintKey); }
        AssertPlacement(grainId);
    }

    internal void AssertPlacement(GrainId grainId)
    {
        var target = Cluster.Silos[PlacementIndex].SiloAddress;
        Cluster.TryGetGrainContext(grainId, out var context).ShouldBeTrue();
        context.ShouldNotBeNull();
        context.Address.SiloAddress.ShouldBe(target);
    }

    public ValueTask DisposeAsync() => Cluster.DisposeAsync();
}
