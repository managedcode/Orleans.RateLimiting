using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using Microsoft.AspNetCore.Http;

namespace ManagedCode.Orleans.RateLimiting.Client.Middlewares;

internal static class RateLimitResponseWriter
{
    public static async Task WriteTooManyRequestsAsync(HttpContext httpContext, OrleansRateLimitLease lease)
    {
        if (httpContext.Response.HasStarted)
            return;

        httpContext.Response.Clear();
        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await httpContext.Response.WriteAsJsonAsync(new RateLimitRejectedResponse(
            StatusCodes.Status429TooManyRequests,
            RateLimitMiddlewareConstants.TooManyRequestsError,
            lease.Reason,
            lease.RetryAfter.ToString()));
    }

    private sealed record RateLimitRejectedResponse(int StatusCode, string Error, string Reason, string RetryAfter);
}
