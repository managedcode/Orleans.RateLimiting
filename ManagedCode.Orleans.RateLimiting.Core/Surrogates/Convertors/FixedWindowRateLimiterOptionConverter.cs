using System.Threading.RateLimiting;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Surrogates.Convertors;

[RegisterConverter]
public sealed class FixedWindowRateLimiterOptionConverter : IConverter<FixedWindowRateLimiterOptions, FixedWindowRateLimiterOptionsSurrogate>
{
    public FixedWindowRateLimiterOptions ConvertFromSurrogate(in FixedWindowRateLimiterOptionsSurrogate surrogate)
    {
        return new FixedWindowRateLimiterOptions
        {
            Window = surrogate.Window,
            AutoReplenishment = surrogate.AutoReplenishment,
            PermitLimit = surrogate.PermitLimit,
            QueueProcessingOrder = surrogate.QueueProcessingOrder,
            QueueLimit = surrogate.QueueLimit
        };
    }

    public FixedWindowRateLimiterOptionsSurrogate ConvertToSurrogate(in FixedWindowRateLimiterOptions value)
    {
        return new FixedWindowRateLimiterOptionsSurrogate
        {
            Window = value.Window,
            AutoReplenishment = value.AutoReplenishment,
            PermitLimit = value.PermitLimit,
            QueueProcessingOrder = value.QueueProcessingOrder,
            QueueLimit = value.QueueLimit
        };
    }
}