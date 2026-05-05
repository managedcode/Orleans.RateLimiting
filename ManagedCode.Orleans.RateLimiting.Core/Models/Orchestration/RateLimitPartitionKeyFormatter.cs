using System;
using System.Globalization;
using System.Linq;

namespace ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;

public static class RateLimitPartitionKeyFormatter
{
    public static string Join(params object?[] parts)
    {
        return string.Join(RateLimitPartitionKeyDefaults.Separator, parts.Select(FormatPart));
    }

    public static string FormatPart(object? part)
    {
        var value = Convert.ToString(part, CultureInfo.InvariantCulture) ?? string.Empty;

        return value
            .Replace(RateLimitPartitionKeyDefaults.EscapeToken, RateLimitPartitionKeyDefaults.EscapedEscapeToken, StringComparison.Ordinal)
            .Replace(RateLimitPartitionKeyDefaults.Separator, RateLimitPartitionKeyDefaults.EscapedSeparator, StringComparison.Ordinal);
    }
}
