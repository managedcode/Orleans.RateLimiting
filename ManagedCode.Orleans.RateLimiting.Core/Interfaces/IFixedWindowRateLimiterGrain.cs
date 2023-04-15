using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace ManagedCode.Orleans.RateLimiting.Core.Interfaces;

public interface IFixedWindowRateLimiterGrain : IRateLimiterGrain
{
    ValueTask ConfigureAsync(FixedWindowRateLimiterOptions options);
    ValueTask<bool> TryReplenishAsync();
}