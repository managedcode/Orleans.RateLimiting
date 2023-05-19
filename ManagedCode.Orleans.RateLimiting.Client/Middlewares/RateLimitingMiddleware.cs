using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
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

public class RateLimitingMiddleware
{
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RequestDelegate _next;
    private readonly IClusterClient _client;
    private readonly IServiceProvider _services;

    public RateLimitingMiddleware(ILogger<RateLimitingMiddleware> logger, RequestDelegate next, IClusterClient client, IServiceProvider services)
    {
        _logger = logger;
        _next = next;
        _client = client;
        _services = services;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        await using var holder = new GroupLimiterHolder();
        
        AddIpRateLimiter(httpContext, holder);
        AddAnonymousIpRateLimiter(httpContext, holder);
        AddAuthorizedIpRateLimiter(httpContext, holder);
        
        await holder.AcquireAsync();
        await _next(httpContext);
    }


    void AddIpRateLimiter(HttpContext httpContext, GroupLimiterHolder holder)
    {
        holder.AddLimiter(TryGetLimiterHolder<IpRateLimiterAttribute>(httpContext, httpContext.Request.GetClientIpAddress()));
    }
    
    void AddAnonymousIpRateLimiter(HttpContext httpContext, GroupLimiterHolder holder)
    {
        if(httpContext.User?.Identity?.IsAuthenticated is not true)
            holder.AddLimiter(TryGetLimiterHolder<AnonymousIpRateLimiterAttribute>(httpContext, httpContext.Request.GetClientIpAddress()));
    }
    
    void AddAuthorizedIpRateLimiter(HttpContext httpContext, GroupLimiterHolder holder)
    {
        if(httpContext.User?.Identity?.IsAuthenticated is true)
            holder.AddLimiter(TryGetLimiterHolder<AuthorizedIpRateLimiterAttribute>(httpContext, 
                CreateKey(httpContext.User.Identity.Name,httpContext.Request.GetClientIpAddress())));

    }
    
    
    private ILimiterHolder? TryGetLimiterHolder<T>(HttpContext httpContext, string key) where T : Attribute, IRateLimiterAttribute
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

        if (attribute != null)
        {
            var limiter = _client.GetRateLimiterByConfig(CreateKey(key,postfix), attribute.ConfigurationName, _services.GetService<IEnumerable<RateLimiterConfig>>());
           
            if(limiter is null)
                _logger.LogError($"Configuration {attribute.ConfigurationName} not found for RateLimiter");
            
            return limiter;
        }
        
        return null;
    }
    
    string CreateKey(string key, string postfix)
    {
        return $"{key}:{postfix}";
    }
}