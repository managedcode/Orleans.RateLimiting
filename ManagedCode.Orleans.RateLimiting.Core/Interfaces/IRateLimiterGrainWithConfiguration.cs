using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Models;

namespace ManagedCode.Orleans.RateLimiting.Core.Interfaces;

public interface IRateLimiterGrainWithConfiguration<TOption> : IRateLimiterGrain
{
    ValueTask ConfigureAsync(TOption options);
    ValueTask<TOption> GetConfiguration();

    Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(TOption options);
    Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(int permitCount, TOption options);
}