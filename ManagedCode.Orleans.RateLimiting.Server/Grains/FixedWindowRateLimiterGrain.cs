using System.Threading.RateLimiting;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;

namespace ManagedCode.Orleans.RateLimiting.Server.Grains;

[Reentrant]
public class FixedWindowRateLimiterGrain : RateLimiterGrain<FixedWindowRateLimiter>, IFixedWindowRateLimiterGrain
{
    public FixedWindowRateLimiterGrain(ILogger<FixedWindowRateLimiterGrain> logger) : base(logger)
    {
    }

    public ValueTask ConfigureAsync(FixedWindowRateLimiterOptions options)
    {
        RateLimiter = new FixedWindowRateLimiter(options);
        _logger.LogInformation($"Configured {nameof(FixedWindowRateLimiter)} with id:{this.GetPrimaryKeyString()}");
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> TryReplenishAsync()
    {
        return ValueTask.FromResult(RateLimiter.TryReplenish());
    }
}