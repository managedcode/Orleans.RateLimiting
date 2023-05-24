using System;
using ManagedCode.Orleans.RateLimiting.Client.Attributes;
using ManagedCode.Orleans.RateLimiting.Client.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Client.Middlewares;

public class OrleansIpRateLimitingMiddleware : OrleansBaseRateLimitingMiddleware
{
    public OrleansIpRateLimitingMiddleware(ILogger<OrleansIpRateLimitingMiddleware> logger, IClusterClient client, IServiceProvider services, RequestDelegate next) :
        base(logger, next, client, services)
    {
    }


    protected override void AddLimiters(HttpContext httpContext, GroupLimiterHolder holder)
    {
        AddIpRateLimiter(httpContext, holder);
    }

    private bool AddIpRateLimiter(HttpContext httpContext, GroupLimiterHolder holder)
    {
        var attribute = TryGetAttribute<IpRateLimiterAttribute>(httpContext);
        if (attribute.HasValue)
            return holder.AddLimiter(TryGetLimiterHolder(CreateKey(httpContext.Request.GetClientIpAddress(), attribute.Value.postfix!),
                attribute.Value.attribute.ConfigurationName));

        return false;
    }
}