using System;
using System.Threading.RateLimiting;

namespace ManagedCode.Orleans.RateLimiting.Core.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ConcurrencyLimiterAttribute : Attribute, ILimiterAttribute<ConcurrencyLimiterOptions>
{
    public string? Key { get; }
    public KeyType KeyType { get; }
    public ConcurrencyLimiterOptions? Options { get; }

    /// <summary>
    /// ConcurrencyLimiterAttribute
    /// </summary>
    /// <param name="keyType">What key to use to identify RateLimiting, can be overridden by setting key property</param>
    /// <param name="key">Custom string as key for RateLimiting</param>
    /// <param name="permitLimit">
    /// Maximum number of permits that can be leased concurrently.
    /// Must be set to a value > 0 by the time these options are passed to the constructor of <see cref="ConcurrencyLimiter"/>.
    /// </param>
    /// <param name="queueLimit">
    /// Maximum number of permits that can be queued concurrently.
    /// Must be set to a value >= 0 by the time these options are passed to the constructor of <see cref="ConcurrencyLimiter"/>.
    /// </param>
    /// <param name="queueProcessingOrder">Determines the behaviour of <see cref="RateLimiter.AcquireAsync"/> when not enough resources can be leased.</param>
    public ConcurrencyLimiterAttribute(KeyType keyType = KeyType.GrainId, string key = default, 
        int permitLimit = default,
        int queueLimit = 0,
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
        
        if (permitLimitNullable.HasValue)
        {
            Options = new ConcurrencyLimiterOptions()
            {
                PermitLimit = permitLimitNullable ?? 1,
                QueueLimit = queueLimit,
                QueueProcessingOrder = queueProcessingOrder
            };
        }
    }
}