using System;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Interfaces;

public interface IRateLimiterGrain : IGrainWithStringKey
{
    Task<RateLimitLeaseMetadata> AcquireAsync(int permitCount = 1);
    ValueTask<RateLimiterStatistics?> GetStatisticsAsync();
    ValueTask ReleaseLease(Guid guid);
}