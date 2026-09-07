using ManagedCode.Orleans.RateLimiting.Tests.TestApp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.TestingHost;
using TUnit.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Client.Extensions;

namespace ManagedCode.Orleans.RateLimiting.Tests.Cluster;

public class TestClusterApplication : IAsyncInitializer, IAsyncDisposable
{
    private const string TestEnvironment = "Development";
    private IHost _host = null!;
    private bool _disposed;

    public async Task InitializeAsync()
    {
        var builder = new InProcessTestClusterBuilder();
        builder.ConfigureSilo((_, silo) => new TestSiloConfigurations().Configure(silo));
        builder.ConfigureClient(client => client.AddOrleansRateLimiting());
        Cluster = builder.Build();
        await Cluster.DeployAsync();

        _host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseEnvironment(TestEnvironment);
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddSingleton(Cluster.Client);
                    HttpHostProgram.ConfigureServices(services);
                });
                webBuilder.Configure(HttpHostProgram.Configure);
            })
            .Build();
        await _host.StartAsync();
    }

    public InProcessTestCluster Cluster { get; private set; } = null!;

    public TestServer Server => _host.GetTestServer();

    public HttpClient CreateClient() => _host.GetTestClient();

    public HubConnection CreateSignalRClient(string hubUrl, Action<HubConnectionBuilder>? configure = null)
    {
        var builder = new HubConnectionBuilder();
        configure?.Invoke(builder);
        return builder.WithUrl(new Uri(Server.BaseAddress, hubUrl), o => o.HttpMessageHandlerFactory = _ => Server.CreateHandler()).Build();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        await _host.StopAsync();
        _host.Dispose();
        await Cluster.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
