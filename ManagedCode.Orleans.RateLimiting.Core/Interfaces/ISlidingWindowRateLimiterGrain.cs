using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Orleans.Concurrency;

namespace ManagedCode.Orleans.RateLimiting.Core.Interfaces;

public interface ISlidingWindowRateLimiterGrain : IRateLimiterGrainWithConfiguration<SlidingWindowRateLimiterOptions>
{
    [AlwaysInterleave]
    ValueTask<bool> TryReplenishAsync();
}
