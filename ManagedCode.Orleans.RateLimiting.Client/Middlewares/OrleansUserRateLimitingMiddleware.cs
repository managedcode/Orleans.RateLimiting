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
    protected OrleansUserRateLimitingMiddleware(ILogger logger, RequestDelegate next, IClusterClient client, IServiceProvider services) : base(logger, next, client,
        services)
    {
    }


    protected override void AddLimiters(HttpContext httpContext, GroupLimiterHolder holder)
    {
        AddAnonymousIpRateLimiter(httpContext, holder);

        // if user is authenticated add in role limiter
        if (!AddInRoleIpRateLimiter(httpContext, holder))
            // if user is not authenticated add authorized limiter
            AddAuthorizedIpRateLimiter(httpContext, holder);
    }

    private bool AddAnonymousIpRateLimiter(HttpContext httpContext, GroupLimiterHolder holder)
    {
        if (httpContext.User?.Identity?.IsAuthenticated is not true)
        {
            var attribute = TryGetAttribute<AnonymousIpRateLimiterAttribute>(httpContext);
            if (attribute.HasValue)
                return holder.AddLimiter(TryGetLimiterHolder(httpContext, CreateKey(httpContext.Request.GetClientIpAddress(), attribute.Value.postfix!),
                    attribute.Value.postfix!));
        }

        return false;
    }

    private bool AddAuthorizedIpRateLimiter(HttpContext httpContext, GroupLimiterHolder holder)
    {
        if (httpContext.User?.Identity?.IsAuthenticated is true)
        {
            var attribute = TryGetAttribute<AuthorizedIpRateLimiterAttribute>(httpContext);
            if (attribute.HasValue)
                return holder.AddLimiter(TryGetLimiterHolder(httpContext,
                    CreateKey(httpContext.Request.GetClientIpAddress(), httpContext.User.Identity.Name!, attribute.Value.postfix!), attribute.Value.postfix!));
        }

        return false;
    }

    private bool AddInRoleIpRateLimiter(HttpContext httpContext, GroupLimiterHolder holder)
    {
        var attribute = TryGetAttribute<InRoleIpRateLimiterAttribute>(httpContext);
        if (attribute.HasValue)
            if (httpContext.User?.Identity?.IsAuthenticated is true && httpContext.User.IsInRole(attribute.Value.attribute.Role))
                return holder.AddLimiter(TryGetLimiterHolder(httpContext,
                    CreateKey(httpContext.Request.GetClientIpAddress(), httpContext.User.Identity.Name!, attribute.Value.attribute.Role, attribute.Value.postfix!),
                    attribute.Value.postfix!));

        return false;
    }
}