using System.Threading.RateLimiting;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;

namespace ManagedCode.Orleans.RateLimiting.Server.Grains;

[Reentrant]
public class SlidingWindowRateLimiterGrain : RateLimiterGrain<SlidingWindowRateLimiter>, ISlidingWindowRateLimiterGrain
{
    public SlidingWindowRateLimiterGrain(ILogger<SlidingWindowRateLimiterGrain> logger) : base(logger)
    {
    }

    public ValueTask ConfigureAsync(SlidingWindowRateLimiterOptions options)
    {
        RateLimiter = new SlidingWindowRateLimiter(options);
        _logger.LogInformation($"Configured {nameof(SlidingWindowRateLimiter)} with id:{this.GetPrimaryKeyString()}");
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> TryReplenishAsync()
    {
        return ValueTask.FromResult(RateLimiter.TryReplenish());
    }
}