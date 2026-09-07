using System.Net;
using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Client.Attributes;
using ManagedCode.Orleans.RateLimiting.Client.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ManagedCode.Orleans.RateLimiting.Tests;

[ClassDataSource<TestClusterApplication>(Shared = SharedType.PerTestSession)]
public class AttributePolicyIsolationSecurityTests(TestClusterApplication testApp)
{
    private const string Path = "/combined-security-policy";
    private const int IpPermitLimit = 1;
    private const int AnonymousPermitLimit = 2;
    private const int QueueLimit = 0;
    private const int WindowMinutes = 10;

    [Test]
    public async Task DifferentAttributeConfigurationsCannotResetEachOthersQuota()
    {
        var ipConfig = Guid.NewGuid().ToString();
        var anonymousConfig = Guid.NewGuid().ToString();
        using var host = new HostBuilder().ConfigureWebHost(web => web.UseTestServer().ConfigureServices(services =>
        {
            services.AddRouting();
            services.AddSingleton(testApp.Cluster.Client);
            services.AddOrleansRateLimiting();
            services.AddOrleansRateLimiterOptions(ipConfig, CreateOptions(IpPermitLimit));
            services.AddOrleansRateLimiterOptions(anonymousConfig, CreateOptions(AnonymousPermitLimit));
        }).Configure(app =>
        {
            app.UseRouting();
            app.UseOrleansIpRateLimiting();
            app.UseOrleansUserRateLimiting();
            app.UseEndpoints(endpoints => endpoints.MapGet(Path, () => Results.Ok()).WithDisplayName(ipConfig)
                .WithMetadata(new IpRateLimiterAttribute(ipConfig), new AnonymousIpRateLimiterAttribute(anonymousConfig)));
        })).Start();
        using var client = host.GetTestClient();

        using var first = await client.GetAsync(Path);
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var second = await client.GetAsync(Path);
        second.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    private static FixedWindowRateLimiterOptions CreateOptions(int permitLimit) => new()
    {
        PermitLimit = permitLimit,
        QueueLimit = QueueLimit,
        Window = TimeSpan.FromMinutes(WindowMinutes)
    };
}
