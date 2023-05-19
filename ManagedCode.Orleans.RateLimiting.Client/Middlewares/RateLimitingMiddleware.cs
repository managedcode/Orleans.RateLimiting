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

        // if user is authenticated add in role limiter
        if (!AddInRoleIpRateLimiter(httpContext, holder))
        {
            // if user is not authenticated add authorized limiter
            AddAuthorizedIpRateLimiter(httpContext, holder);
        }
        
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


    private bool AddIpRateLimiter(HttpContext httpContext, GroupLimiterHolder holder)
    {
        var attribute = TryGetAttribute<IpRateLimiterAttribute>(httpContext);
        if (attribute.HasValue)
        {
            return holder.AddLimiter(TryGetLimiterHolder(httpContext, CreateKey(httpContext.Request.GetClientIpAddress(), attribute.Value.postfix!),
                attribute.Value.postfix!));
        }
        
        return false;
    }
    
    private bool AddAnonymousIpRateLimiter(HttpContext httpContext, GroupLimiterHolder holder)
    {
        if (httpContext.User?.Identity?.IsAuthenticated is not true)
        {
            var attribute = TryGetAttribute<AnonymousIpRateLimiterAttribute>(httpContext);
            if (attribute.HasValue)
            {
                return holder.AddLimiter(TryGetLimiterHolder(httpContext, CreateKey(httpContext.Request.GetClientIpAddress(), attribute.Value.postfix!),
                    attribute.Value.postfix!));
            }
        }
        
        return false;
    }
    
    private bool AddAuthorizedIpRateLimiter(HttpContext httpContext, GroupLimiterHolder holder)
    {
        if (httpContext.User?.Identity?.IsAuthenticated is true)
        {
            var attribute = TryGetAttribute<AuthorizedIpRateLimiterAttribute>(httpContext);
            if (attribute.HasValue)
            {
                return holder.AddLimiter(TryGetLimiterHolder(httpContext, 
                    CreateKey(httpContext.Request.GetClientIpAddress(), httpContext.User.Identity.Name!, attribute.Value.postfix!),
                    attribute.Value.postfix!));
            }
        }
        
        return false;
    }

    private bool AddInRoleIpRateLimiter(HttpContext httpContext, GroupLimiterHolder holder)
    {
        var attribute = TryGetAttribute<InRoleIpRateLimiterAttribute>(httpContext);
        if (attribute.HasValue)
        {
            if (httpContext.User?.Identity?.IsAuthenticated is true && httpContext.User.IsInRole(attribute.Value.attribute.Role))
            {
                return holder.AddLimiter(TryGetLimiterHolder(httpContext,
                    CreateKey(httpContext.Request.GetClientIpAddress(), httpContext.User.Identity.Name!, attribute.Value.attribute.Role, attribute.Value.postfix!), 
                    attribute.Value.postfix!));
            }
        }

        return false;
    }


    private (T attribute, string? postfix)? TryGetAttribute<T>(HttpContext httpContext) where T : Attribute, IRateLimiterAttribute
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
    
    private ILimiterHolder? TryGetLimiterHolder(HttpContext httpContext, string key, string configurationName)
    {
        var limiter = _client.GetRateLimiterByConfig(key, configurationName, _services.GetService<IEnumerable<RateLimiterConfig>>());
       
        if(limiter is null)
            _logger.LogError($"Configuration {configurationName} not found for RateLimiter");

        return limiter;
    }
    
    string CreateKey(params string[] parts)
    {
        return string.Join(":", parts);
    }
}