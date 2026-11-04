using System.Net;
using System.Security.Claims;
using System.Text.Json;
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
using Microsoft.Extensions.Primitives;

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
    private const string FirstForwardedHop = "203.0.113.99";
    private const string EmptyHeaderValue = "";
    private const string WhitespaceHeaderValue = " ";
    private const string EmptyFirstHop = ",203.0.113.10";
    private const string MappedPeerIp = "::ffff:198.51.100.5";
    private const string Path = "/protected";
    private const string MissingConfiguration = "missing-security-config";
    private const string AuthenticationType = "security-test";
    private const string UserId = "security-user";
    private const string Role = "security-role";
    private const string SecurityHeader = "X-Content-Type-Options";
    private const string SecurityHeaderValue = "nosniff";
    private const string StatusCodeProperty = "statusCode";
    private const int StaleBodyBytes = 16;
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
    [Arguments(IpMode)]
    [Arguments(OrchestrationMode)]
    public async Task RateLimitedResponsePreservesSecurityHeadersAndRetryDelay(string mode)
    {
        using var host = CreateHost(mode);
        using var client = host.GetTestClient();
        using var accepted = await client.GetAsync(Path);
        accepted.StatusCode.ShouldBe(HttpStatusCode.OK);
        accepted.Headers.GetValues(SecurityHeader).ShouldContain(SecurityHeaderValue);

        using var rejected = await client.GetAsync(Path);
        rejected.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        rejected.Headers.GetValues(SecurityHeader).ShouldContain(SecurityHeaderValue);
        var retryAfter = rejected.Headers.RetryAfter.ShouldNotBeNull();
        retryAfter.Delta.ShouldBe(TimeSpan.FromMinutes(WindowMinutes));
    }

    [Test]
    [Arguments(IpMode)]
    [Arguments(OrchestrationMode)]
    public async Task RateLimitedResponseRewindsTruncatedBodyBuffer(string mode)
    {
        using var host = CreateHost(mode, bufferedResponse: true);
        using var client = host.GetTestClient();
        using var accepted = await client.GetAsync(Path);
        accepted.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var rejected = await client.GetAsync(Path);
        rejected.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        using var body = JsonDocument.Parse(await rejected.Content.ReadAsStringAsync());
        body.RootElement.GetProperty(StatusCodeProperty).GetInt32().ShouldBe(StatusCodes.Status429TooManyRequests);
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
    [Arguments(null, FirstIp, FirstIp)]
    [Arguments(EmptyHeaderValue, FirstIp, FirstIp)]
    [Arguments(WhitespaceHeaderValue, FirstIp, PeerIp)]
    [Arguments(EmptyFirstHop, SecondIp, PeerIp)]
    [Arguments(ForwardedChain, SecondIp, FirstForwardedHop)]
    [Arguments(FirstIp, SecondIp, FirstIp)]
    public void LegacyHeaderSelectionPreservesFirstHopAndFallback(string? first, string? second, string expected)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(PeerIp);
        context.Request.Headers[ForwardedFor] = new StringValues([first, second]);

        context.Request.GetClientIpAddress([ForwardedFor]).ShouldBe(expected);
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
        bool missingAddress = false, bool missingConfiguration = false, bool bufferedResponse = false)
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
            if (bufferedResponse)
                app.Use(BufferResponseAsync);

            app.Use((context, next) =>
            {
                context.Response.Headers[SecurityHeader] = SecurityHeaderValue;
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

    private static async Task BufferResponseAsync(HttpContext context, RequestDelegate next)
    {
        var response = context.Response;
        var original = response.Body;
        await using var buffer = new PositionPreservingBuffer();
        buffer.Write(new byte[StaleBodyBytes]);
        response.Body = buffer;
        try
        {
            await next(context);
        }
        finally
        {
            response.Body = original;
        }

        if (response.StatusCode == StatusCodes.Status429TooManyRequests)
        {
            buffer.Position = default;
            await buffer.CopyToAsync(original);
        }
    }

    private sealed class PositionPreservingBuffer : MemoryStream
    {
        public override void SetLength(long value)
        {
            var position = Position;
            base.SetLength(value);
            Position = position;
        }
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
