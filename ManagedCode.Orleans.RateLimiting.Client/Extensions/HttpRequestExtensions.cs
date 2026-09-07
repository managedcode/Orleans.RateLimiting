using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace ManagedCode.Orleans.RateLimiting.Client.Extensions;

public static class HttpRequestExtensions
{
    private const char CsvSeparator = ',';
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
        StringValues values;

        if (request.Headers?.TryGetValue(headerName, out values) ?? false)
        {
            var rawValues = values.ToString(); // writes out as Csv when there are multiple.

            if (!string.IsNullOrWhiteSpace(rawValues))
            {
                var value = SplitCsv(rawValues).FirstOrDefault();
                return value ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private static IEnumerable<string> SplitCsv(string? csvList)
    {
        if (string.IsNullOrWhiteSpace(csvList))
            return Enumerable.Empty<string>();

        return csvList.TrimEnd(CsvSeparator).Split(CsvSeparator).Select(s => s.Trim());
    }
}
