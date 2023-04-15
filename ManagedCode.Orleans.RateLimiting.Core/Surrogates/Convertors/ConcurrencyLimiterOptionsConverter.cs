using System.Threading.RateLimiting;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Surrogates.Convertors;

[RegisterConverter]
public sealed class
    ConcurrencyLimiterOptionsConverter : IConverter<ConcurrencyLimiterOptions, ConcurrencyLimiterOptionsSurrogate>
{
    public ConcurrencyLimiterOptions ConvertFromSurrogate(in ConcurrencyLimiterOptionsSurrogate surrogate)
    {
        return new ConcurrencyLimiterOptions
        {
            PermitLimit = surrogate.PermitLimit,
            QueueLimit = surrogate.QueueLimit,
            QueueProcessingOrder = surrogate.QueueProcessingOrder
        };
    }

    public ConcurrencyLimiterOptionsSurrogate ConvertToSurrogate(in ConcurrencyLimiterOptions value)
    {
        return new ConcurrencyLimiterOptionsSurrogate
        {
            PermitLimit = value.PermitLimit,
            QueueLimit = value.QueueLimit,
            QueueProcessingOrder = value.QueueProcessingOrder
        };
    }
}