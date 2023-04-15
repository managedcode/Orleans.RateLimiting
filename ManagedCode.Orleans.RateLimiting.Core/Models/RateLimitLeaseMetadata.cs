using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.RateLimiting;
using Orleans;
using Orleans.Runtime;

namespace ManagedCode.Orleans.RateLimiting.Core.Models;

[GenerateSerializer]
public class RateLimitLeaseMetadata
{
    public RateLimitLeaseMetadata(Guid guid, GrainId grainId, RateLimitLease lease)
    {
        LeaseId = guid;
        GrainId = grainId;
        IsAcquired = lease.IsAcquired;
        Metadata = lease.GetAllMetadata().ToArray();
    }

    public RateLimitLeaseMetadata(GrainId grainId)
    {
        LeaseId = Guid.Empty;
        GrainId = grainId;
        IsAcquired = false;
        Metadata = new[] { new KeyValuePair<string, object>("REASON_PHRASE", "Lease not acquired") };
    }

    [Id(0)]
    public Guid LeaseId { get; set; }

    [Id(1)]
    public GrainId GrainId { get; set; }

    [Id(2)]
    public bool IsAcquired { get; set; }

    [Id(3)]
    public KeyValuePair<string, object?>[] Metadata { get; set; }
}