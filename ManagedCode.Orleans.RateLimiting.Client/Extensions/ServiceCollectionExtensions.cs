using ManagedCode.Orleans.RateLimiting.Client.Middlewares;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Orleans.RateLimiting.Client.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrleansRateLimiting(this IServiceCollection collection)
    {
        //collection.AddTransient<OrleansUserRateLimitingMiddleware>();
        //collection.AddTransient<OrleansIpRateLimitingMiddleware>();
        return collection;
    }
}