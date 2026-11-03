using System;
using System.Globalization;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using Microsoft.AspNetCore.Http;

namespace ManagedCode.Orleans.RateLimiting.Client.Middlewares;

internal static class RateLimitResponseWriter
{
    public static async Task WriteTooManyRequestsAsync(HttpContext httpContext, OrleansRateLimitLease lease)
    {
        var response = httpContext.Response;
        if (response.HasStarted)
            return;

        // Replace the response body without removing headers set by the host.
        response.ContentLength = null;
        if (response.Body.CanSeek)
            response.Body.SetLength(default);

        response.StatusCode = StatusCodes.Status429TooManyRequests;
        var retryAfter = lease.RetryAfter;
        if (retryAfter > TimeSpan.Zero)
            response.Headers.RetryAfter = ((long)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);

        await response.WriteAsJsonAsync(new RateLimitRejectedResponse(
            StatusCodes.Status429TooManyRequests,
            RateLimitMiddlewareConstants.TooManyRequestsError,
            lease.Reason,
            retryAfter.ToString(RateLimitMiddlewareConstants.RetryAfterTimeSpanFormat, CultureInfo.InvariantCulture)));
    }

    private sealed record RateLimitRejectedResponse(int StatusCode, string Error, string Reason, string RetryAfter);
}
