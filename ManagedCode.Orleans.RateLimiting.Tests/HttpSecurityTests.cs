using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Client.Attributes;
using ManagedCode.Orleans.RateLimiting.Client.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Exceptions;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ManagedCode.Orleans.RateLimiting.Tests;

[ClassDataSource<TestClusterApplication>(Shared = SharedType.PerTestSession)]
public class HttpSecurityTests(TestClusterApplication testApp)
{
    private const string ForwardedFor = "X-Forwarded-For";
    private const string RealIp = "X-Real-IP";
    private const string RemoteAddress = "REMOTE_ADDR";
    private const string PeerIp = "198.51.100.5";
    private const string FirstIp = "203.0.113.10";
    private const string SecondIp = "203.0.113.11";
    private const string ForwardedChain = "203.0.113.99, 203.0.113.10";
    private const string MappedPeerIp = "::ffff:198.51.100.5";
    private const string Path = "/protected";
    private const string MissingConfiguration = "missing-security-config";
    private const string AuthenticationType = "security-test";
    private const string UserId = "security-user";
    private const string Role = "security-role";
    private const string IpMode = "ip";
    private const string AnonymousMode = "anonymous";
    private const string AuthorizedMode = "authorized";
    private const string RoleMode = "role";
    private const string OrchestrationMode = "orchestration";
    private const int PermitLimit = 1;
    private const int QueueLimit = 0;
    private const int WindowMinutes = 10;

    [Test]
    [Arguments(IpMode)]
    [Arguments(AnonymousMode)]
    [Arguments(AuthorizedMode)]
    [Arguments(RoleMode)]
    [Arguments(OrchestrationMode)]
    public async Task RotatingUntrustedHeadersCannotBypassAnyHttpLimiter(string mode)
    {
        using var host = CreateHost(mode);
        using var client = host.GetTestClient();
        (await SendAsync(client, ForwardedFor, FirstIp)).ShouldBe(HttpStatusCode.OK);
        foreach (var header in new[] { ForwardedFor, RealIp, RemoteAddress })
        {
            (await SendAsync(client, header, SecondIp)).ShouldBe(HttpStatusCode.TooManyRequests);
            (await SendAsync(client, header, ForwardedChain)).ShouldBe(HttpStatusCode.TooManyRequests);
        }
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task OnlyKnownProxiesCanSelectForwardedClientPartitions(bool trustedProxy)
    {
        using var host = CreateHost(OrchestrationMode, forwardedHeaders: true, trustedProxy: trustedProxy);
        using var client = host.GetTestClient();
        (await SendAsync(client, ForwardedFor, FirstIp)).ShouldBe(HttpStatusCode.OK);
        (await SendAsync(client, ForwardedFor, ForwardedChain)).ShouldBe(HttpStatusCode.TooManyRequests);
        (await SendAsync(client, ForwardedFor, SecondIp)).ShouldBe(
            trustedProxy ? HttpStatusCode.OK : HttpStatusCode.TooManyRequests);
    }

    [Test]
    public async Task MissingConnectionAddressDoesNotDisableIpPolicy()
    {
        using var host = CreateHost(OrchestrationMode, missingAddress: true);
        using var client = host.GetTestClient();
        (await SendAsync(client, ForwardedFor, FirstIp)).ShouldBe(HttpStatusCode.OK);
        (await SendAsync(client, RealIp, SecondIp)).ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Test]
    public void MappedIpv4AndNativeIpv4UseTheSameKey()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(MappedPeerIp);
        context.Request.GetClientIpAddress().ShouldBe(PeerIp);
    }

    [Test]
    [Arguments(IpMode)]
    [Arguments(AnonymousMode)]
    [Arguments(AuthorizedMode)]
    [Arguments(RoleMode)]
    public async Task MissingAttributeConfigurationCannotRunProtectedEndpoint(string mode)
    {
        using var host = CreateHost(mode, missingConfiguration: true);
        using var client = host.GetTestClient();
        await Should.ThrowAsync<RateLimitConfigurationNotFoundException>(() => client.GetAsync(Path));
    }

    private IHost CreateHost(string mode, bool forwardedHeaders = false, bool trustedProxy = false,
        bool missingAddress = false, bool missingConfiguration = false)
    {
        var configName = Guid.NewGuid().ToString();
        return new HostBuilder().ConfigureWebHost(web => web.UseTestServer().ConfigureServices(services =>
        {
            services.AddRouting();
            services.AddSingleton(testApp.Cluster.Client);
            services.AddOrleansRateLimiterOptions(configName, new FixedWindowRateLimiterOptions
            {
                PermitLimit = PermitLimit,
                QueueLimit = QueueLimit,
                Window = TimeSpan.FromMinutes(WindowMinutes)
            });
            services.AddOrleansRateLimiting(options =>
            {
                if (mode == OrchestrationMode)
                    options.AddIpAddress(configName);
            });
        }).Configure(app =>
        {
            app.Use((context, next) =>
            {
                context.Connection.RemoteIpAddress = missingAddress ? null : IPAddress.Parse(PeerIp);
                if (mode is AuthorizedMode or RoleMode)
                    context.User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, UserId), new Claim(ClaimTypes.Role, Role)], AuthenticationType));
                return next(context);
            });
            if (forwardedHeaders)
                app.UseForwardedHeaders(CreateForwardedOptions(trustedProxy));
            app.UseRouting();
            app.UseOrleansIpRateLimiting();
            app.UseOrleansUserRateLimiting();
            app.UseOrleansRequestRateLimiting();
            app.UseEndpoints(endpoints => endpoints.MapGet(Path, () => Results.Ok()).WithMetadata(
                CreateAttribute(mode, missingConfiguration ? MissingConfiguration : configName)).WithDisplayName(configName));
        })).Start();
    }

    private static ForwardedHeadersOptions CreateForwardedOptions(bool trustedProxy)
    {
        var options = new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor };
        if (trustedProxy)
            options.KnownProxies.Add(IPAddress.Parse(PeerIp));
        return options;
    }

    private static object CreateAttribute(string mode, string configName) => mode switch
    {
        IpMode => new IpRateLimiterAttribute(configName),
        AnonymousMode => new AnonymousIpRateLimiterAttribute(configName),
        AuthorizedMode => new AuthorizedIpRateLimiterAttribute(configName),
        RoleMode => new InRoleIpRateLimiterAttribute(configName, Role),
        _ => new object()
    };

    private static async Task<HttpStatusCode> SendAsync(HttpClient client, string header, string value)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, Path);
        request.Headers.Add(header, value);
        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }
}
