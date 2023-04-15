using System.Threading.RateLimiting;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;

namespace ManagedCode.Orleans.RateLimiting.Server.Grains;

[Reentrant]
public class TokenBucketRateLimiterGrain : RateLimiterGrain<TokenBucketRateLimiter>, ITokenBucketRateLimiterGrain
{
    public TokenBucketRateLimiterGrain(ILogger<TokenBucketRateLimiterGrain> logger) : base(logger)
    {
    }

    public ValueTask ConfigureAsync(TokenBucketRateLimiterOptions options)
    {
        RateLimiter = new TokenBucketRateLimiter(options);
        _logger.LogInformation($"Configured {nameof(TokenBucketRateLimiter)} with id:{this.GetPrimaryKeyString()}");
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> TryReplenishAsync()
    {
        return ValueTask.FromResult(RateLimiter.TryReplenish());
    }
}