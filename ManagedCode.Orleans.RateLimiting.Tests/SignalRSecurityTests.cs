using System.Net;
using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Client.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ManagedCode.Orleans.RateLimiting.Tests;

[ClassDataSource<TestClusterApplication>(Shared = SharedType.PerTestSession)]
public class SignalRSecurityTests(TestClusterApplication testApp)
{
    private const string HubPath = "/security-hub";
    private const string ForwardedFor = "X-Forwarded-For";
    private const string RealIp = "X-Real-IP";
    private const string FirstSpoofedIp = "203.0.113.10";
    private const string SecondSpoofedIp = "203.0.113.11";
    private const string PeerIp = "198.51.100.7";
    private const int PermitLimit = 1;
    private const int QueueLimit = 0;
    private const int WindowMinutes = 10;

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task ReconnectingWithSpoofedHeadersDoesNotResetIpQuota(bool missingAddress)
    {
        using var host = CreateHost(missingAddress);
        await using var first = CreateConnection(host.GetTestServer(), FirstSpoofedIp);
        await first.StartAsync();
        (await first.InvokeAsync<bool>(nameof(SecurityHub.Protected))).ShouldBeTrue();
        await first.StopAsync();

        await using var second = CreateConnection(host.GetTestServer(), SecondSpoofedIp);
        await second.StartAsync();
        await Should.ThrowAsync<HubException>(() => second.InvokeAsync<bool>(nameof(SecurityHub.Protected)));
    }

    private IHost CreateHost(bool missingAddress)
    {
        var configurationName = Guid.NewGuid().ToString();
        return new HostBuilder().ConfigureWebHost(web => web.UseTestServer().ConfigureServices(services =>
        {
            services.AddRouting();
            services.AddSingleton(testApp.Cluster.Client);
            services.AddOrleansRateLimiterOptions(configurationName, new FixedWindowRateLimiterOptions
            {
                PermitLimit = PermitLimit,
                QueueLimit = QueueLimit,
                Window = TimeSpan.FromMinutes(WindowMinutes)
            });
            services.AddSignalR().AddOrleansRateLimiting(configurationName, RateLimitPartitionKind.IpAddress);
        }).Configure(app =>
        {
            app.Use((context, next) =>
            {
                context.Connection.RemoteIpAddress = missingAddress ? null : IPAddress.Parse(PeerIp);
                return next(context);
            });
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapHub<SecurityHub>(HubPath));
        })).Start();
    }

    private static HubConnection CreateConnection(TestServer server, string spoofedIp) => new HubConnectionBuilder()
        .WithUrl(new Uri(server.BaseAddress, HubPath), options =>
        {
            options.Transports = HttpTransportType.LongPolling;
            options.HttpMessageHandlerFactory = _ => server.CreateHandler();
            options.Headers[ForwardedFor] = spoofedIp;
            options.Headers[RealIp] = spoofedIp;
        }).Build();

    public sealed class SecurityHub : Hub
    {
        public bool Protected() => true;
    }
}
