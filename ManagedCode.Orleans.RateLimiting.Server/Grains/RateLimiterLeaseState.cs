using System;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Server.Grains;

[GenerateSerializer]
public sealed class RateLimiterLeaseState
{
    private const int LeaseIdMemberId = 0;
    private const int PermitCountMemberId = 1;

    public RateLimiterLeaseState()
    {
    }

    public RateLimiterLeaseState(Guid leaseId, int permitCount)
    {
        LeaseId = leaseId;
        PermitCount = permitCount;
    }

    [Id(LeaseIdMemberId)]
    public Guid LeaseId { get; set; }

    [Id(PermitCountMemberId)]
    public int PermitCount { get; set; }
}
