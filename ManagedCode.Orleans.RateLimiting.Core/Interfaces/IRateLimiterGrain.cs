using System;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using Orleans;
using Orleans.Concurrency;

namespace ManagedCode.Orleans.RateLimiting.Core.Interfaces;

public interface IRateLimiterGrain : IGrainWithStringKey
{
    [AlwaysInterleave]
    Task<RateLimitLeaseMetadata> AcquireAsync(int permitCount = 1);

    [AlwaysInterleave]
    ValueTask<RateLimiterStatistics?> GetStatisticsAsync();

    [AlwaysInterleave]
    ValueTask ReleaseLease(Guid leaseId);

    ValueTask ResetAsync();
    ValueTask DeleteStateAsync();
}
