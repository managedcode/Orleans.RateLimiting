using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace ManagedCode.Orleans.RateLimiting.Core.Interfaces;

public interface ITokenBucketRateLimiterGrain : IRateLimiterGrain
{
    ValueTask ConfigureAsync(TokenBucketRateLimiterOptions options);
    ValueTask<bool> TryReplenishAsync();
}