using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace ManagedCode.Orleans.RateLimiting.Core.Interfaces;

public interface ISlidingWindowRateLimiterGrain : IRateLimiterGrain
{
    ValueTask ConfigureAsync(SlidingWindowRateLimiterOptions options);
    ValueTask<bool> TryReplenishAsync();
}