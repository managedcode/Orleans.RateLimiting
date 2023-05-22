using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Orleans.RateLimiting.Client.Attributes;
using ManagedCode.Orleans.RateLimiting.Client.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Client.Middlewares;

public abstract class OrleansBaseRateLimitingMiddleware
{
    private readonly ILogger _logger;
    private readonly RequestDelegate _next;
    private readonly IClusterClient _client;
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
        
        // throw too many requests if any of the limiters is null code 429
        var error = await holder.AcquireAsync();
        if (error is null)
        {
            await _next(httpContext);
        }
        else
        {
            httpContext.Response.Clear();
            httpContext.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            await httpContext.Response.WriteAsJsonAsync(Result.Fail(HttpStatusCode.TooManyRequests,error.ToException()));
        }
    }
    
    protected (T attribute, string? postfix)? TryGetAttribute<T>(HttpContext httpContext) where T : Attribute, IRateLimiterAttribute
    {
        var endpoint = httpContext.GetEndpoint();
        
        if(endpoint is null)
            return null;
        
        // first try to get attribute from endpoint, 
        var attribute = endpoint.Metadata.GetMetadata<T>();
        string postfix = endpoint.ToString()!;
        
        if (attribute is null)
        {
            // then try to get attribute from controller
            var controllerType = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>()?.ControllerTypeInfo;

            if (controllerType != null)
            {
                attribute = controllerType.GetCustomAttribute<T>(inherit: true);
                postfix = controllerType.ToString();
            }
        }

        if (attribute is null)
            return null;
        
        return (attribute, postfix);
    }
    
    protected ILimiterHolder? TryGetLimiterHolder(HttpContext httpContext, string key, string configurationName)
    {
        var limiter = _client.GetRateLimiterByConfig(key, configurationName, _services.GetService<IEnumerable<RateLimiterConfig>>());
       
        if(limiter is null)
            _logger.LogError($"Configuration {configurationName} not found for RateLimiter");

        return limiter;
    }
    
    protected string CreateKey(params string[] parts)
    {
        return string.Join(":", parts);
    }
}