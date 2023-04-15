using System.Threading.RateLimiting;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Surrogates.Convertors;

[RegisterConverter]
public sealed class
    TokenBucketRateLimiterOptionsConverter : IConverter<TokenBucketRateLimiterOptions,
        TokenBucketRateLimiterOptionsSurrogate>
{
    public TokenBucketRateLimiterOptions ConvertFromSurrogate(in TokenBucketRateLimiterOptionsSurrogate surrogate)
    {
        return new TokenBucketRateLimiterOptions
        {
            AutoReplenishment = surrogate.AutoReplenishment,
            QueueLimit = surrogate.QueueLimit,
            QueueProcessingOrder = surrogate.QueueProcessingOrder,
            ReplenishmentPeriod = surrogate.ReplenishmentPeriod,
            TokenLimit = surrogate.TokenLimit,
            TokensPerPeriod = surrogate.TokensPerPeriod
        };
    }

    public TokenBucketRateLimiterOptionsSurrogate ConvertToSurrogate(in TokenBucketRateLimiterOptions value)
    {
        return new TokenBucketRateLimiterOptionsSurrogate
        {
            AutoReplenishment = value.AutoReplenishment,
            QueueLimit = value.QueueLimit,
            QueueProcessingOrder = value.QueueProcessingOrder,
            ReplenishmentPeriod = value.ReplenishmentPeriod,
            TokenLimit = value.TokenLimit,
            TokensPerPeriod = value.TokensPerPeriod
        };
    }
}