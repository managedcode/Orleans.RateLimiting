using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace ManagedCode.Orleans.RateLimiting.Core.Interfaces;

public interface ISlidingWindowRateLimiterGrain : IRateLimiterGrainWithConfiguration<SlidingWindowRateLimiterOptions>
{
    ValueTask<bool> TryReplenishAsync();
}