using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace ManagedCode.Orleans.RateLimiting.Core.Interfaces;

public interface IFixedWindowRateLimiterGrain : IRateLimiterGrainWithConfiguration<FixedWindowRateLimiterOptions>
{
    ValueTask<bool> TryReplenishAsync();
}