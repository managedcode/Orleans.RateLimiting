using System.Diagnostics;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Client.Middlewares;

public class RateLimitingMiddleware
{
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RequestDelegate _next;
    private readonly IClusterClient _client;

    public RateLimitingMiddleware(ILogger<RateLimitingMiddleware> logger, RequestDelegate next, IClusterClient client)
    {
        _logger = logger;
        _next = next;
        _client = client;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        var limiter = _client.GetFixedWindowRateLimiter(httpContext.User.Identity.Name);
        
        await using var lease = await limiter.AcquireAsync();
        lease.ThrowIfNotAcquired();
        await _next(httpContext);

    }
}