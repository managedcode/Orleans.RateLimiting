using System;
using System.Globalization;
using System.Linq;

namespace ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;

public static class RateLimitPartitionKeyFormatter
{
    private const int EscapeExpansion = 2;
    private static readonly char EscapeCharacter = RateLimitPartitionKeyDefaults.EscapeToken[0];
    private static readonly char SeparatorCharacter = RateLimitPartitionKeyDefaults.Separator[0];

    public static string Join(params object?[] parts)
    {
        return parts switch
        {
            [var first, var second] => Join(first, second),
            [var first, var second, var third] => Join(first, second, third),
            [var first, var second, var third, var fourth] => string.Join(RateLimitPartitionKeyDefaults.Separator,
                (ReadOnlySpan<string?>)[FormatPart(first), FormatPart(second), FormatPart(third), FormatPart(fourth)]),
            [var first, var second, var third, var fourth, var fifth] => string.Join(RateLimitPartitionKeyDefaults.Separator,
                (ReadOnlySpan<string?>)[FormatPart(first), FormatPart(second), FormatPart(third), FormatPart(fourth), FormatPart(fifth)]),
            [var first, var second, var third, var fourth, var fifth, var sixth] => string.Join(RateLimitPartitionKeyDefaults.Separator,
                (ReadOnlySpan<string?>)[FormatPart(first), FormatPart(second), FormatPart(third), FormatPart(fourth), FormatPart(fifth), FormatPart(sixth)]),
            _ => string.Join(RateLimitPartitionKeyDefaults.Separator, parts.Select(FormatPart))
        };
    }

    internal static string Join(object? first, object? second)
        => string.Concat(FormatPart(first), RateLimitPartitionKeyDefaults.Separator, FormatPart(second));

    internal static string Join(object? first, object? second, object? third)
        => string.Join(RateLimitPartitionKeyDefaults.Separator,
            (ReadOnlySpan<string?>)[FormatPart(first), FormatPart(second), FormatPart(third)]);

    public static string FormatPart(object? part)
    {
        var value = Convert.ToString(part, CultureInfo.InvariantCulture) ?? string.Empty;
        var input = value.AsSpan();
        var first = input.IndexOfAny(EscapeCharacter, SeparatorCharacter);
        if (first < 0)
            return value;

        var tail = input[first..];
        var escapeCount = tail.Count(EscapeCharacter);
        if (escapeCount == 0)
            return value.Replace(RateLimitPartitionKeyDefaults.Separator, RateLimitPartitionKeyDefaults.EscapedSeparator, StringComparison.Ordinal);

        var separatorCount = tail.Count(SeparatorCharacter);
        if (separatorCount == 0)
            return value.Replace(RateLimitPartitionKeyDefaults.EscapeToken, RateLimitPartitionKeyDefaults.EscapedEscapeToken, StringComparison.Ordinal);

        var length = checked(value.Length + (escapeCount + separatorCount) * EscapeExpansion);
        return string.Create(length, (value, first), static (destination, state) =>
        {
            var input = state.value.AsSpan();
            input[..state.first].CopyTo(destination);
            var index = state.first;
            foreach (var character in input[index..])
            {
                var escaped = character == EscapeCharacter ? RateLimitPartitionKeyDefaults.EscapedEscapeToken
                    : character == SeparatorCharacter ? RateLimitPartitionKeyDefaults.EscapedSeparator : null;
                if (escaped is null)
                {
                    destination[index++] = character;
                    continue;
                }

                escaped.AsSpan().CopyTo(destination[index..]);
                index += escaped.Length;
            }
        });
    }
}
