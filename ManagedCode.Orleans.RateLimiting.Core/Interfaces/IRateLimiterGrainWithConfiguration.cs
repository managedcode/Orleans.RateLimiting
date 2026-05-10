using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using Orleans.Concurrency;

namespace ManagedCode.Orleans.RateLimiting.Core.Interfaces;

public interface IRateLimiterGrainWithConfiguration<TOption> : IRateLimiterGrain
{
    ValueTask ConfigureAsync(TOption options);
    ValueTask<TOption> GetConfiguration();

    [AlwaysInterleave]
    Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(TOption options);

    [AlwaysInterleave]
    Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(int permitCount, TOption options);
}
