using System;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Client.Middlewares;

public class RateLimitingHubFilter : IHubFilter
{
    private readonly IClusterClient _client;
    private readonly ILogger<RateLimitingHubFilter> _logger;

    public RateLimitingHubFilter(ILogger<RateLimitingHubFilter> logger, IClusterClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var userName = invocationContext.Context.User?.Identity?.Name;

        if (string.IsNullOrWhiteSpace(userName))
        {
            _logger.LogDebug("Skipping rate limiter acquisition because no user identity was provided for {Hub}", invocationContext.Hub?.GetType().FullName);
            return await next(invocationContext);
        }

        var limiter = _client.GetFixedWindowRateLimiter(userName);

        await using var lease = await limiter.AcquireAsync();
        lease.ThrowIfNotAcquired();
        return await next(invocationContext);
    }

    // Optional method
    public Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        return next(context);
    }

    // Optional method
    public Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next)
    {
        return next(context, exception);
    }
}