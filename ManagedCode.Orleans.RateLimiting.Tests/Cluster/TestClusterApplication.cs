using ManagedCode.Orleans.RateLimiting.Tests.TestApp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.TestingHost;

namespace ManagedCode.Orleans.RateLimiting.Tests.Cluster;

public class TestClusterApplication : IDisposable, IAsyncDisposable
{
    private readonly IHost _host;
    private bool _disposed;

    public TestClusterApplication()
    {
        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<TestSiloConfigurations>();
        builder.AddClientBuilderConfigurator<TestClientConfigurations>();
        Cluster = builder.Build();
        Cluster.Deploy();

        _host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseEnvironment("Development");
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddSingleton(Cluster.Client);
                    HttpHostProgram.ConfigureServices(services);
                });
                webBuilder.Configure(HttpHostProgram.Configure);
            })
            .Start();
    }

    public TestCluster Cluster { get; }

    public TestServer Server => _host.GetTestServer();

    public HttpClient CreateClient() => _host.GetTestClient();

    public HubConnection CreateSignalRClient(string hubUrl, Action<HubConnectionBuilder>? configure = null)
    {
        var builder = new HubConnectionBuilder();
        configure?.Invoke(builder);
        return builder.WithUrl(new Uri(Server.BaseAddress, hubUrl), o => o.HttpMessageHandlerFactory = _ => Server.CreateHandler()).Build();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _host.Dispose();
        Cluster.Dispose();
        GC.SuppressFinalize(this);
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
