using System.Threading.RateLimiting;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;

namespace ManagedCode.Orleans.RateLimiting.Server.Grains;

[Reentrant]
public class ConcurrencyLimiterGrain : RateLimiterGrain<ConcurrencyLimiter>, IConcurrencyLimiterGrain
{
    public ConcurrencyLimiterGrain(ILogger<ConcurrencyLimiterGrain> logger) : base(logger)
    {
    }

    public ValueTask ConfigureAsync(ConcurrencyLimiterOptions options)
    {
        RateLimiter = new ConcurrencyLimiter(options);
        _logger.LogInformation($"Configured {nameof(ConcurrencyLimiter)} with id:{this.GetPrimaryKeyString()}");
        return ValueTask.CompletedTask;
    }
}