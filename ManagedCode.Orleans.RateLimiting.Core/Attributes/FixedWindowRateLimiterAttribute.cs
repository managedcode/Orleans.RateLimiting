using System;
using System.Threading.RateLimiting;

namespace ManagedCode.Orleans.RateLimiting.Core.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class FixedWindowRateLimiterAttribute : Attribute, ILimiterAttribute<FixedWindowRateLimiterOptions>
{
    public string? Key { get; }
    public KeyType KeyType { get; }
    public FixedWindowRateLimiterOptions? Options { get; }

    /// <summary>
    /// FixedWindowRateLimiterAttribute
    /// </summary>
    /// <param name="keyType">What key to use to identify RateLimiting, can be overridden by setting key property</param>
    /// <param name="key">Custom string as key for RateLimiting</param>
    /// <param name="windowInSeconds">
    /// Specifies the time window that takes in the requests in seconds.
    /// Must be set to a value greater than <see cref="TimeSpan.Zero" /> by the time these options are passed to the constructor of <see cref="FixedWindowRateLimiter"/>.
    /// </param>
    /// <param name="permitLimit">
    /// Maximum number of permits that can be leased concurrently.
    /// Must be set to a value > 0 by the time these options are passed to the constructor of <see cref="FixedWindowRateLimiter"/>.
    /// </param>
    /// <param name="queueLimit">
    /// Maximum number of permits that can be queued concurrently.
    /// Must be set to a value >= 0 by the time these options are passed to the constructor of <see cref="FixedWindowRateLimiter"/>.
    /// </param>
    /// <param name="autoReplenishment">
    /// Maximum number of permit counters that can be allowed in a window.
    /// Must be set to a value > 0 by the time these options are passed to the constructor of <see cref="FixedWindowRateLimiter"/>.
    /// </param>
    /// <param name="queueProcessingOrder">Determines the behaviour of <see cref="RateLimiter.AcquireAsync"/> when not enough resources can be leased.</param>
    public FixedWindowRateLimiterAttribute(KeyType keyType = KeyType.GrainId, string key = default, 
        int windowInSeconds = default,
        int permitLimit = default,
        int queueLimit = 0,
        bool autoReplenishment = true,
        QueueProcessingOrder queueProcessingOrder = QueueProcessingOrder.OldestFirst)
    {
        Key = key;
        KeyType = keyType;
        
        //override keyType if key is set
        if (!string.IsNullOrEmpty(key))
        {
            KeyType = KeyType.Key;
        }
        
        int? permitLimitNullable = permitLimit > 0 ? permitLimit : null;
        TimeSpan? windowNullable = windowInSeconds > 0 ? TimeSpan.FromSeconds(windowInSeconds) : null;
        
        if (permitLimitNullable.HasValue || windowNullable.HasValue)
        {
            Options = new FixedWindowRateLimiterOptions()
            {
                Window = windowNullable ?? TimeSpan.FromSeconds(1),
                PermitLimit = permitLimitNullable ?? 1,
                QueueLimit = queueLimit,
                AutoReplenishment = autoReplenishment,
                QueueProcessingOrder = queueProcessingOrder
            };
        }
    }
}