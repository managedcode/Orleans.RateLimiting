using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Client.Options;
using ManagedCode.Orleans.RateLimiting.Client.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Exceptions;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ManagedCode.Orleans.RateLimiting.Client.Middlewares;

public class RateLimitingHubFilter : IHubFilter
{
    private const int MetadataEntryCount = 4;
    private static readonly Action<ILogger, string, string, Exception?> LogRateLimited =
        LoggerMessage.Define<string, string>(LogLevel.Information, default, RateLimitMiddlewareConstants.SignalRRateLimitedLogMessage);

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
        await using var holder = await _orchestrator.CreateLimiterGroupAsync(CreateContext(invocationContext), invocationContext.Context.ConnectionAborted);
        var lease = await holder.AcquireAsync(invocationContext.Context.ConnectionAborted);
        if (lease is not null)
        {
            var reason = lease.Reason;
            LogRateLimited(_logger, invocationContext.HubMethodName, reason, null);
            throw new RateLimitExceededException(reason, lease.RetryAfter);
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

        var hubType = invocationContext.Hub.GetType();
        var hubName = hubType.FullName;
        var methodName = invocationContext.HubMethodName;
        var context = new RateLimitRequestContext
        {
            OperationName = methodName,
            UserId = userKey,
            GroupId = user?.FindFirstValue(RateLimitMiddlewareConstants.GroupClaimType)
                      ?? user?.FindFirstValue(RateLimitMiddlewareConstants.GroupsClaimType)
                      ?? user?.FindFirstValue(ClaimTypes.GroupSid),
            TenantId = user?.FindFirstValue(RateLimitMiddlewareConstants.TenantIdClaimType)
                       ?? user?.FindFirstValue(RateLimitMiddlewareConstants.ShortTenantIdClaimType),
            Role = user?.FindFirstValue(ClaimTypes.Role),
            IpAddress = httpContext?.Request.GetClientIpAddress(),
            Resource = string.Concat(hubName, RateLimitMiddlewareConstants.ResourceSeparator, methodName),
            PolicyName = _options.PolicyName
        };

        var metadata = context.Metadata;
        metadata.EnsureCapacity(MetadataEntryCount);
        metadata[RateLimitMiddlewareConstants.HubMetadataKey] = hubName ?? hubType.Name;
        metadata[RateLimitMiddlewareConstants.MethodMetadataKey] = methodName;
        metadata[RateLimitMiddlewareConstants.ConfigurationMetadataKey] = _options.ConfigurationName;
        metadata[RateLimitMiddlewareConstants.PartitionMetadataKey] = _options.PartitionKind.ToString();
        return context;
    }
}
