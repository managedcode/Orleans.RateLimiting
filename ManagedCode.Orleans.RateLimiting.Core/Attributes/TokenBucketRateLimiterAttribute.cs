using System;
using System.Threading.RateLimiting;

namespace ManagedCode.Orleans.RateLimiting.Core.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class TokenBucketRateLimiterAttribute : Attribute, ILimiterAttribute<TokenBucketRateLimiterOptions>
{
    public string? Key { get; }
    public KeyType KeyType { get; }
    public TokenBucketRateLimiterOptions? Options { get; }

    /// <summary>
    /// FixedWindowRateLimiterAttribute
    /// </summary>
    /// <param name="keyType">What key to use to identify RateLimiting, can be overridden by setting key property</param>
    /// <param name="key">Custom string as key for RateLimiting</param>
    /// <param name="replenishmentPeriodInSeconds">
    /// Specifies the minimum period between replenishments in seconds.
    /// Must be set to a value greater than <see cref="TimeSpan.Zero" /> by the time these options are passed to the constructor of <see cref="TokenBucketRateLimiter"/>.
    /// </param>
    /// <param name="tokensPerPeriod">
    /// Specifies the maximum number of tokens to restore each replenishment.
    /// Must be set to a value > 0 by the time these options are passed to the constructor of <see cref="TokenBucketRateLimiter"/>.
    /// </param>
    /// <param name="queueLimit">
    /// Maximum number of permits that can be queued concurrently.
    /// Must be set to a value >= 0 by the time these options are passed to the constructor of <see cref="TokenBucketRateLimiter"/>.
    /// </param>
    /// <param name="tokenLimit">
    /// Specified whether the <see cref="TokenBucketRateLimiter"/> is automatically replenishing tokens or if someone else
    /// will be calling <see cref="TokenBucketRateLimiter.TryReplenish"/> to replenish tokens.
    /// </param>
    /// <param name="autoReplenishment">
    /// Maximum number of permit counters that can be allowed in a window.
    /// Must be set to a value > 0 by the time these options are passed to the constructor of <see cref="TokenBucketRateLimiter"/>.
    /// </param>
    /// <param name="queueProcessingOrder">Determines the behaviour of <see cref="RateLimiter.AcquireAsync"/> when not enough resources can be leased.</param>
    public TokenBucketRateLimiterAttribute(KeyType keyType = KeyType.GrainId, string key = default, 
        int replenishmentPeriodInSeconds = default,
        int tokensPerPeriod = default,
        int queueLimit = 0,
        int tokenLimit = default,
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
        
        int? tokensPerPeriodNullable = tokensPerPeriod > 0 ? tokensPerPeriod : null;
        int? tokenLimitNullable = tokenLimit > 0 ? tokenLimit : null;
        TimeSpan? replenishmentPeriodNullable  = replenishmentPeriodInSeconds > 0 ? TimeSpan.FromSeconds(replenishmentPeriodInSeconds) : null;
        
        if (tokensPerPeriodNullable.HasValue || tokenLimitNullable.HasValue || replenishmentPeriodNullable.HasValue)
        {
            Options = new TokenBucketRateLimiterOptions()
            {
                ReplenishmentPeriod = replenishmentPeriodNullable ?? TimeSpan.FromSeconds(1),
                TokensPerPeriod = tokensPerPeriodNullable ?? 1,
                QueueLimit = queueLimit,
                TokenLimit = tokenLimitNullable ?? 1,
                AutoReplenishment = autoReplenishment,
                QueueProcessingOrder = queueProcessingOrder
            };
        }
    }
}