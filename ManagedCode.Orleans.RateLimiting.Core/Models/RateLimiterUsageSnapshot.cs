using System;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Models;

/// <summary>Usage captured by the limiter in the same state mutation as a lease decision or configuration reset.</summary>
[GenerateSerializer]
public readonly record struct RateLimiterUsageSnapshot
{
    private const int TotalSuccessfulLeasesMemberId = 0;
    private const int TotalFailedLeasesMemberId = 1;
    private const int CurrentAvailablePermitsMemberId = 2;
    private const int PermitLimitMemberId = 3;
    private const int ObservedAtUtcMemberId = 4;

    [Id(TotalSuccessfulLeasesMemberId)]
    public long TotalSuccessfulLeases { get; init; }

    [Id(TotalFailedLeasesMemberId)]
    public long TotalFailedLeases { get; init; }

    [Id(CurrentAvailablePermitsMemberId)]
    public int CurrentAvailablePermits { get; init; }

    [Id(PermitLimitMemberId)]
    public int PermitLimit { get; init; }

    /// <summary>The UTC observation timestamp, ordered by the limiter even when the clock has not advanced.</summary>
    [Id(ObservedAtUtcMemberId)]
    public DateTimeOffset ObservedAtUtc { get; init; }
}
