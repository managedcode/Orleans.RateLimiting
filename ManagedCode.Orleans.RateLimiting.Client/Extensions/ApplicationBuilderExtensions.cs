using ManagedCode.Orleans.RateLimiting.Client.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace ManagedCode.Orleans.RateLimiting.Client.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder AddOrleansRateLimiting(this IApplicationBuilder applicationBuilder)
    {
        applicationBuilder.UseMiddleware<RateLimitingMiddleware>();
        return applicationBuilder;
    }
}