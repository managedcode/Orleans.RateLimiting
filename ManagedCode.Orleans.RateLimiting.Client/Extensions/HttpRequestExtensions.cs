using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace ManagedCode.Orleans.RateLimiting.Client.Extensions;

public static class HttpRequestExtensions
{
    public static string GetClientIpAddress(this HttpRequest request)
    {
        return GetClientIpAddress(request, new []
        {
            "X-Real-IP",
            "X-Forwarded-For",
            "REMOTE_ADDR"
        });
    }
    
    public static string GetClientIpAddress(this HttpRequest request, string[] headers)
    {
        string? ip = null;
        
        foreach (var header in headers)
        {
            ip = GetHeaderValueAs(request, header);
            if(!string.IsNullOrEmpty(ip))
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
            string rawValues = values.ToString(); // writes out as Csv when there are multiple.

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

        return csvList
            .TrimEnd(',')
            .Split(',')
            .Select(s => s.Trim());
    }
}