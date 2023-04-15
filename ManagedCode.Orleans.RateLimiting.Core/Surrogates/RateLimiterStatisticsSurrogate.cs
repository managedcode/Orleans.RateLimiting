using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Surrogates;

[GenerateSerializer]
public struct RateLimiterStatisticsSurrogate
{
    [Id(0)]
    public long CurrentAvailablePermits { get; set; }

    [Id(1)]
    public long CurrentQueuedCount { get; set; }

    [Id(2)]
    public long TotalFailedLeases { get; set; }

    [Id(3)]
    public long TotalSuccessfulLeases { get; set; }
}