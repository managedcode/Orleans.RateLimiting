using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Client.Options;
using ManagedCode.Orleans.RateLimiting.Core.Exceptions;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ManagedCode.Orleans.RateLimiting.Client.Middlewares;

public class RateLimitingHubFilter : IHubFilter
{
    private readonly ILogger<RateLimitingHubFilter> _logger;
    private readonly IRateLimitRequestOrchestrator _orchestrator;
    private readonly SignalRRateLimitingOptions _options;

    public RateLimitingHubFilter(
        ILogger<RateLimitingHubFilter> logger,
        IRateLimitRequestOrchestrator orchestrator,
        IOptions<SignalRRateLimitingOptions> options)
    {
        _logger = logger;
        _orchestrator = orchestrator;
        _options = options.Value;
    }

    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        await using var holder = await _orchestrator.CreateLimiterGroupAsync(CreateContext(invocationContext));
        var lease = await holder.AcquireAsync();
        if (lease is not null)
        {
            _logger.LogInformation(RateLimitMiddlewareConstants.SignalRRateLimitedLogMessage, invocationContext.HubMethodName, lease.Reason);
            throw new RateLimitExceededException(lease.Reason, lease.RetryAfter);
        }

        return await next(invocationContext);
    }

    public Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        return next(context);
    }

    public Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next)
    {
        return next(context, exception);
    }

    private RateLimitRequestContext CreateContext(HubInvocationContext invocationContext)
    {
        var httpContext = invocationContext.Context.GetHttpContext();
        var user = invocationContext.Context.User;
        var userKey = invocationContext.Context.UserIdentifier
                      ?? user?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? user?.Identity?.Name
                      ?? _options.AnonymousUserKey;

        return new RateLimitRequestContext
        {
            OperationName = invocationContext.HubMethodName,
            UserId = userKey,
            GroupId = user?.FindFirstValue(RateLimitMiddlewareConstants.GroupClaimType)
                      ?? user?.FindFirstValue(RateLimitMiddlewareConstants.GroupsClaimType)
                      ?? user?.FindFirstValue(ClaimTypes.GroupSid),
            TenantId = user?.FindFirstValue(RateLimitMiddlewareConstants.TenantIdClaimType)
                       ?? user?.FindFirstValue(RateLimitMiddlewareConstants.ShortTenantIdClaimType),
            Role = user?.FindFirstValue(ClaimTypes.Role),
            IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
            Resource = string.Concat(
                invocationContext.Hub.GetType().FullName,
                RateLimitMiddlewareConstants.ResourceSeparator,
                invocationContext.HubMethodName),
            Metadata = new Dictionary<string, string>
            {
                [RateLimitMiddlewareConstants.HubMetadataKey] = invocationContext.Hub.GetType().FullName ?? invocationContext.Hub.GetType().Name,
                [RateLimitMiddlewareConstants.MethodMetadataKey] = invocationContext.HubMethodName,
                [RateLimitMiddlewareConstants.ConfigurationMetadataKey] = _options.ConfigurationName,
                [RateLimitMiddlewareConstants.PartitionMetadataKey] = _options.PartitionKind.ToString()
            },
            PolicyName = _options.PolicyName
        };
    }
}
