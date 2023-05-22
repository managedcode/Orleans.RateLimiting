using ManagedCode.Orleans.RateLimiting.Client.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace ManagedCode.Orleans.RateLimiting.Client.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseOrleansIpRateLimiting(this IApplicationBuilder applicationBuilder)
    {
        applicationBuilder.UseMiddleware<OrleansIpRateLimitingMiddleware>();
        return applicationBuilder;
    }

    public static IApplicationBuilder UseOrleansUserRateLimiting(this IApplicationBuilder applicationBuilder)
    {
        applicationBuilder.UseMiddleware<OrleansUserRateLimitingMiddleware>();
        return applicationBuilder;
    }
}