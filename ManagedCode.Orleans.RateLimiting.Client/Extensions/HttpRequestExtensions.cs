using System;
using Microsoft.AspNetCore.Http;

namespace ManagedCode.Orleans.RateLimiting.Client.Extensions;

public static class HttpRequestExtensions
{
    private const char CsvSeparator = ',';
    private const int SeparatorNotFound = -1;
    private const string UnknownIpAddress = "unknown-ip";

    /// <summary>
    /// Returns the connection address after any trusted proxy middleware has run.
    /// Raw request headers are never used by this overload.
    /// </summary>
    public static string GetClientIpAddress(this HttpRequest request)
    {
        var address = request.HttpContext.Connection.RemoteIpAddress;
        if (address is null)
            return UnknownIpAddress;

        return (address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address).ToString();
    }

    /// <summary>
    /// Reads explicitly selected headers. Only use this legacy overload after independently
    /// validating the proxy and ensuring it overwrites these headers. Header names do not
    /// establish trust. Prefer the parameterless overload with ForwardedHeadersMiddleware.
    /// </summary>
    public static string GetClientIpAddress(this HttpRequest request, string[] headers)
    {
        string? ip = null;

        foreach (var header in headers)
        {
            ip = GetHeaderValueAs(request, header);
            if (!string.IsNullOrEmpty(ip))
                break;
        }

        if (string.IsNullOrEmpty(ip) && request.HttpContext?.Connection?.RemoteIpAddress != null)
            ip = request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

        return ip ?? string.Empty;
    }

    private static string GetHeaderValueAs(HttpRequest request, string headerName)
    {
        if (request.Headers is not { } headers || !headers.TryGetValue(headerName, out var values))
            return string.Empty;

        foreach (var value in values)
        {
            // StringValues joins only nonempty entries. Only the first entry can supply the first hop.
            if (string.IsNullOrEmpty(value))
                continue;

            var separator = value.IndexOf(CsvSeparator);
            return separator == SeparatorNotFound
                ? value.Trim()
                : value.AsSpan()[..separator].Trim().ToString();
        }

        return string.Empty;
    }
}
