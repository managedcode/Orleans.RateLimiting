using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Client.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;
using Microsoft.AspNetCore.Http;

namespace ManagedCode.Orleans.RateLimiting.Client.Middlewares;

public sealed class OrleansRequestRateLimitingMiddleware
{
    private readonly IRateLimitRequestOrchestrator _orchestrator;
    private readonly RequestDelegate _next;
    private readonly string? _policyName;

    public OrleansRequestRateLimitingMiddleware(RequestDelegate next, IRateLimitRequestOrchestrator orchestrator)
        : this(next, orchestrator, null)
    {
    }

    public OrleansRequestRateLimitingMiddleware(RequestDelegate next, IRateLimitRequestOrchestrator orchestrator, string? policyName)
    {
        _next = next;
        _orchestrator = orchestrator;
        _policyName = policyName;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        await using var holder = await _orchestrator.CreateLimiterGroupAsync(CreateContext(httpContext), httpContext.RequestAborted);
        var error = await holder.AcquireAsync();

        if (error is null)
        {
            await _next(httpContext);
            return;
        }

        if (httpContext.Response.HasStarted)
            return;

        httpContext.Response.Clear();
        httpContext.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        await httpContext.Response.WriteAsJsonAsync(new
        {
            StatusCode = (int)HttpStatusCode.TooManyRequests,
            Error = "Too many requests",
            error.Reason,
            RetryAfter = error.RetryAfter.ToString()
        });
    }

    private RateLimitRequestContext CreateContext(HttpContext httpContext)
    {
        var endpoint = httpContext.GetEndpoint();
        var user = httpContext.User;
        var path = httpContext.Request.Path.Value ?? "/";

        return new RateLimitRequestContext
        {
            OperationName = endpoint?.DisplayName ?? $"{httpContext.Request.Method} {path}",
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.Identity?.Name,
            GroupId = user.FindFirstValue("group") ?? user.FindFirstValue("groups") ?? user.FindFirstValue(ClaimTypes.GroupSid),
            TenantId = user.FindFirstValue("tenant_id") ?? user.FindFirstValue("tid"),
            Role = user.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Role)?.Value,
            IpAddress = httpContext.Request.GetClientIpAddress(),
            Resource = path,
            Metadata = new Dictionary<string, string>
            {
                ["method"] = httpContext.Request.Method,
                ["path"] = path
            },
            PolicyName = _policyName
        };
    }
}
