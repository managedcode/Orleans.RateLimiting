using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Surrogates;

[Immutable]
[GenerateSerializer]
public struct RateLimiterStatisticsSurrogate
{
    [Id(0)] public long CurrentAvailablePermits;

    [Id(1)] public long CurrentQueuedCount;

    [Id(2)] public long TotalFailedLeases;

    [Id(3)] public long TotalSuccessfulLeases;
}