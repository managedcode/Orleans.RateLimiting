using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Client.Attributes;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;
using ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Client.Middlewares;

public abstract class OrleansBaseRateLimitingMiddleware
{
    private readonly IClusterClient _client;
    private readonly ILogger _logger;
    private readonly RequestDelegate _next;
    private readonly IServiceProvider _services;

    protected OrleansBaseRateLimitingMiddleware(ILogger logger, RequestDelegate next, IClusterClient client, IServiceProvider services)
    {
        _logger = logger;
        _next = next;
        _client = client;
        _services = services;
    }

    protected abstract void AddLimiters(HttpContext httpContext, GroupLimiterHolder holder);

    public async Task Invoke(HttpContext httpContext)
    {
        await using var holder = new GroupLimiterHolder();

        AddLimiters(httpContext, holder);

        var error = await holder.AcquireAsync();
        if (error is null)
        {
            await _next(httpContext);
        }
        else
        {
            await RateLimitResponseWriter.WriteTooManyRequestsAsync(httpContext, error);
        }
    }

    protected static (T attribute, string? postfix)? TryGetAttribute<T>(HttpContext httpContext) where T : Attribute, IRateLimiterPolicy
    {
        var endpoint = httpContext.GetEndpoint();

        if (endpoint is null)
            return null;

        // first try to get attribute from endpoint, 
        var attribute = endpoint.Metadata.GetMetadata<T>();
        var postfix = endpoint.ToString()!;

        if (attribute is null)
        {
            // then try to get attribute from controller
            var controllerType = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>()?.ControllerTypeInfo;

            if (controllerType != null)
            {
                attribute = controllerType.GetCustomAttribute<T>(true);
                postfix = controllerType.ToString();
            }
        }

        if (attribute is null)
            return null;

        return (attribute, postfix);
    }

    protected ILimiterHolder? TryGetLimiterHolder(string key, string configurationName)
    {
        var limiter = _client.GetRateLimiterByConfig(key, configurationName, _services.GetServices<RateLimiterConfig>());

        if (limiter is null)
            _logger.LogError(RateLimitMiddlewareConstants.ConfigurationNotFoundLogMessage, configurationName);

        return limiter;
    }

    protected static string CreateKey(params string[] parts)
    {
        return RateLimitPartitionKeyFormatter.Join(parts);
    }
}
