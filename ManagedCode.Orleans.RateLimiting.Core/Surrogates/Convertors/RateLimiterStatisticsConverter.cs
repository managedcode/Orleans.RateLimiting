using System.Threading.RateLimiting;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Surrogates.Convertors;

[RegisterConverter]
public sealed class RateLimiterStatisticsConverter : IConverter<RateLimiterStatistics, RateLimiterStatisticsSurrogate>
{
    public RateLimiterStatistics ConvertFromSurrogate(in RateLimiterStatisticsSurrogate surrogate)
    {
        return new RateLimiterStatistics
        {
            CurrentAvailablePermits = surrogate.CurrentAvailablePermits,
            CurrentQueuedCount = surrogate.CurrentQueuedCount,
            TotalFailedLeases = surrogate.TotalFailedLeases,
            TotalSuccessfulLeases = surrogate.TotalSuccessfulLeases
        };
    }

    public RateLimiterStatisticsSurrogate ConvertToSurrogate(in RateLimiterStatistics value)
    {
        return new RateLimiterStatisticsSurrogate
        {
            CurrentAvailablePermits = value.CurrentAvailablePermits,
            CurrentQueuedCount = value.CurrentQueuedCount,
            TotalFailedLeases = value.TotalFailedLeases,
            TotalSuccessfulLeases = value.TotalSuccessfulLeases
        };
    }
}