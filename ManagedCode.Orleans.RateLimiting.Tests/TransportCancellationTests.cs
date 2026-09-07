using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Client.Middlewares;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;
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
public class TransportCancellationTests(TestClusterApplication testApp)
{
    private const string HttpPath = "/cancel-http";
    private const string HubPath = "/cancel-hub";
    private const int PermitCount = 1;
    private const int QueueLimit = 1;
    private const int EmptyQueue = 0;
    private const int TimeoutSeconds = 10;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(TimeoutSeconds);

    [Test]
    public async Task HttpAbortCancelsQueuedGrainCallBeforeEndpointExecutes()
    {
        var holder = CreateHolder();
        await using var held = await holder.AcquireAndConfigureAsync();
        var calls = new InvocationCounter();
        using var host = CreateHost(holder, calls);
        using var client = host.GetTestClient();
        using var cancellation = new CancellationTokenSource();
        var pending = client.GetAsync(HttpPath, cancellation.Token);
        await CancellationTests.WaitForQueueAsync(holder, PermitCount);
        await cancellation.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(() => pending.WaitAsync(Timeout));
        await CancellationTests.WaitForQueueAsync(holder, EmptyQueue);
        calls.Count.ShouldBe(EmptyQueue);
        await held.DisposeAsync();
        (await client.GetAsync(HttpPath)).IsSuccessStatusCode.ShouldBeTrue();
        calls.Count.ShouldBe(PermitCount);
    }

    [Test]
    public async Task SignalRDisconnectCancelsQueuedGrainCallBeforeHubExecutes()
    {
        var holder = CreateHolder();
        await using var held = await holder.AcquireAndConfigureAsync();
        var calls = new InvocationCounter();
        using var host = CreateHost(holder, calls);
        var server = host.GetTestServer();
        await using var connection = new HubConnectionBuilder().WithUrl(new Uri(server.BaseAddress, HubPath), options =>
        {
            options.Transports = HttpTransportType.LongPolling;
            options.HttpMessageHandlerFactory = _ => server.CreateHandler();
        }).Build();
        await connection.StartAsync();
        var pending = connection.InvokeAsync<bool>(nameof(CancellationHub.Protected));
        await CancellationTests.WaitForQueueAsync(holder, PermitCount);
        await connection.StopAsync().WaitAsync(Timeout);
        await Should.ThrowAsync<Exception>(() => pending.WaitAsync(Timeout));
        await CancellationTests.WaitForQueueAsync(holder, EmptyQueue);
        calls.Count.ShouldBe(EmptyQueue);
        await held.DisposeAsync();
        (await holder.GetStatisticsAsync())!.CurrentAvailablePermits.ShouldBe(PermitCount);
    }

    private ConcurrencyLimiterHolder CreateHolder() => testApp.Cluster.Client.GetConcurrencyLimiter(Guid.NewGuid().ToString(),
        new ConcurrencyLimiterOptions { PermitLimit = PermitCount, QueueLimit = QueueLimit });

    private static IHost CreateHost(ILimiterHolder holder, InvocationCounter calls) => new HostBuilder()
        .ConfigureWebHost(web => web.UseTestServer().ConfigureServices(services =>
        {
            services.AddRouting();
            services.AddSingleton(calls);
            services.AddSingleton<IRateLimitRequestOrchestrator>(new FixedOrchestrator(holder));
            services.AddSignalR(options => options.AddFilter<RateLimitingHubFilter>());
        }).Configure(app =>
        {
            app.UseRouting();
            app.UseWhen(context => context.Request.Path == HttpPath,
                branch => branch.UseMiddleware<OrleansRequestRateLimitingMiddleware>());
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet(HttpPath, () => calls.Invoke());
                endpoints.MapHub<CancellationHub>(HubPath);
            });
        })).Start();

    private sealed class FixedOrchestrator(ILimiterHolder holder) : IRateLimitRequestOrchestrator
    {
        public ValueTask<GroupLimiterHolder> CreateLimiterGroupAsync(RateLimitRequestContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var group = new GroupLimiterHolder();
            group.AddLimiter(holder);
            return ValueTask.FromResult(group);
        }
    }

    public sealed class InvocationCounter
    {
        private int _count;
        public int Count => Volatile.Read(ref _count);
        public bool Invoke() { Interlocked.Increment(ref _count); return true; }
    }

    public sealed class CancellationHub(InvocationCounter calls) : Hub
    {
        public bool Protected() => calls.Invoke();
    }
}
