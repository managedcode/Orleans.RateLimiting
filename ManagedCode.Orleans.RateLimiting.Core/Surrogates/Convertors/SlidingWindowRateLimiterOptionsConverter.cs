using System.Threading.RateLimiting;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Surrogates.Convertors;

[RegisterConverter]
public sealed class SlidingWindowRateLimiterOptionsConverter : IConverter<SlidingWindowRateLimiterOptions, SlidingWindowRateLimiterOptionsSurrogate>
{
    public SlidingWindowRateLimiterOptions ConvertFromSurrogate(in SlidingWindowRateLimiterOptionsSurrogate surrogate)
    {
        return new SlidingWindowRateLimiterOptions
        {
            QueueProcessingOrder = surrogate.QueueProcessingOrder,
            PermitLimit = surrogate.PermitLimit,
            QueueLimit = surrogate.QueueLimit,
            Window = surrogate.Window,
            AutoReplenishment = surrogate.AutoReplenishment,
            SegmentsPerWindow = surrogate.SegmentsPerWindow
        };
    }

    public SlidingWindowRateLimiterOptionsSurrogate ConvertToSurrogate(in SlidingWindowRateLimiterOptions value)
    {
        return new SlidingWindowRateLimiterOptionsSurrogate
        {
            QueueProcessingOrder = value.QueueProcessingOrder,
            PermitLimit = value.PermitLimit,
            QueueLimit = value.QueueLimit,
            Window = value.Window,
            AutoReplenishment = value.AutoReplenishment,
            SegmentsPerWindow = value.SegmentsPerWindow
        };
    }
}