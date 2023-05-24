using System;
using ManagedCode.Orleans.RateLimiting.Client.Attributes;
using ManagedCode.Orleans.RateLimiting.Client.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Client.Middlewares;

public class OrleansUserRateLimitingMiddleware : OrleansBaseRateLimitingMiddleware
{
    public OrleansUserRateLimitingMiddleware(ILogger<OrleansUserRateLimitingMiddleware> logger, IClusterClient client, IServiceProvider services, RequestDelegate next) 
        : base(logger, next, client, services)
    {
    }


    protected override void AddLimiters(HttpContext httpContext, GroupLimiterHolder holder)
    {
        AddAnonymousRateLimiter(httpContext, holder);

        // if user is authenticated add in role limiter
        if (!AddInRoleRateLimiter(httpContext, holder))
            // if user is not authenticated add authorized limiter
            AddAuthorizedRateLimiter(httpContext, holder);
    }

    private bool AddAnonymousRateLimiter(HttpContext httpContext, GroupLimiterHolder holder)
    {
        if (httpContext.User?.Identity?.IsAuthenticated is not true)
        {
            var attribute = TryGetAttribute<AnonymousIpRateLimiterAttribute>(httpContext);
            if (attribute.HasValue)
                return holder.AddLimiter(TryGetLimiterHolder(CreateKey(httpContext.Request.GetClientIpAddress(), attribute.Value.postfix!),
                    attribute.Value.attribute.ConfigurationName));
        }

        return false;
    }

    private bool AddAuthorizedRateLimiter(HttpContext httpContext, GroupLimiterHolder holder)
    {
        if (httpContext.User?.Identity?.IsAuthenticated is true)
        {
            var attribute = TryGetAttribute<AuthorizedIpRateLimiterAttribute>(httpContext);
            if (attribute.HasValue)
                return holder.AddLimiter(TryGetLimiterHolder(CreateKey(httpContext.Request.GetClientIpAddress(), httpContext.User.Identity.Name ?? "rate-user-name", attribute.Value.postfix!), attribute.Value.attribute.ConfigurationName));
        }

        return false;
    }

    private bool AddInRoleRateLimiter(HttpContext httpContext, GroupLimiterHolder holder)
    {
        var attribute = TryGetAttribute<InRoleIpRateLimiterAttribute>(httpContext);
        if (attribute.HasValue)
            if (httpContext.User?.Identity?.IsAuthenticated is true && httpContext.User.IsInRole(attribute.Value.attribute.Role))
                return holder.AddLimiter(TryGetLimiterHolder(CreateKey(httpContext.Request.GetClientIpAddress(), httpContext.User.Identity.Name!, attribute.Value.attribute.Role, attribute.Value.postfix!),
                    attribute.Value.attribute.ConfigurationName));

        return false;
    }
}