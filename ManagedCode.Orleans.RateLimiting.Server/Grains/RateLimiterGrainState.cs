using System;
using System.Collections.Generic;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Server.Grains;

[GenerateSerializer]
public sealed class RateLimiterGrainState<TOptions>
{
    private const int HasConfigurationMemberId = 0;
    private const int OptionsMemberId = 1;
    private const int HasSnapshotMemberId = 2;
    private const int CurrentAvailablePermitsMemberId = 3;
    private const int TotalSuccessfulLeasesMemberId = 4;
    private const int TotalFailedLeasesMemberId = 5;
    private const int UpdatedAtUtcMemberId = 6;
    private const int ActiveLeasesMemberId = 7;

    [Id(HasConfigurationMemberId)]
    public bool HasConfiguration { get; set; }

    [Id(OptionsMemberId)]
    public TOptions? Options { get; set; }

    [Id(HasSnapshotMemberId)]
    public bool HasSnapshot { get; set; }

    [Id(CurrentAvailablePermitsMemberId)]
    public int CurrentAvailablePermits { get; set; }

    [Id(TotalSuccessfulLeasesMemberId)]
    public long TotalSuccessfulLeases { get; set; }

    [Id(TotalFailedLeasesMemberId)]
    public long TotalFailedLeases { get; set; }

    [Id(UpdatedAtUtcMemberId)]
    public DateTimeOffset UpdatedAtUtc { get; set; }

    [Id(ActiveLeasesMemberId)]
    public List<RateLimiterLeaseState> ActiveLeases { get; set; } = [];
}
