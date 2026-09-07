using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;

namespace ManagedCode.Orleans.RateLimiting.Server.GrainCallFilter;

internal static class RateLimiterPolicyKeys
{
    private const string NamedPolicy = "named-policy";
    private const string InlinePolicy = "inline-policy";

    public static string ForNamedPolicy(string key, string configurationName) =>
        RateLimitPartitionKeyFormatter.Join(NamedPolicy, configurationName.ToUpperInvariant(), key);

    public static string ForInlinePolicy(string key, object? options)
    {
        var settings = options switch
        {
            ConcurrencyLimiterOptions value => RateLimitPartitionKeyFormatter.Join(
                value.PermitLimit, value.QueueLimit, value.QueueProcessingOrder),
            FixedWindowRateLimiterOptions value => RateLimitPartitionKeyFormatter.Join(
                value.PermitLimit, value.QueueLimit, value.QueueProcessingOrder, value.Window.Ticks, value.AutoReplenishment),
            SlidingWindowRateLimiterOptions value => RateLimitPartitionKeyFormatter.Join(
                value.PermitLimit, value.QueueLimit, value.QueueProcessingOrder, value.Window.Ticks, value.AutoReplenishment, value.SegmentsPerWindow),
            TokenBucketRateLimiterOptions value => RateLimitPartitionKeyFormatter.Join(
                value.TokenLimit, value.TokensPerPeriod, value.QueueLimit, value.QueueProcessingOrder, value.ReplenishmentPeriod.Ticks, value.AutoReplenishment),
            _ => null
        };

        // Preserve key ownership for default options and externally implemented filter types.
        return settings is null ? key : RateLimitPartitionKeyFormatter.Join(InlinePolicy, settings, key);
    }
}
