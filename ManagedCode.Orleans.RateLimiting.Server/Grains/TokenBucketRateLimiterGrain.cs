using System.Threading.RateLimiting;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Concurrency;

namespace ManagedCode.Orleans.RateLimiting.Server.Grains;

[Reentrant]
public class TokenBucketRateLimiterGrain : RateLimiterGrain<TokenBucketRateLimiter, TokenBucketRateLimiterOptions>,
    ITokenBucketRateLimiterGrain
{
    public TokenBucketRateLimiterGrain(ILogger<TokenBucketRateLimiterGrain> logger,
        IOptions<TokenBucketRateLimiterOptions> options) : base(logger, options.Value)
    {
    }
    
    public ValueTask<bool> TryReplenishAsync()
    {
        return ValueTask.FromResult(RateLimiter.TryReplenish());
    }

    protected override TokenBucketRateLimiter CreateDefaultRateLimiter()
    {
        return new TokenBucketRateLimiter(Options);
    }
    
    private bool CheckOptions(TokenBucketRateLimiterOptions options)
    {
        return Options.TokenLimit != options.TokenLimit
               || Options.QueueLimit != options.QueueLimit
               || Options.QueueProcessingOrder != options.QueueProcessingOrder
               || Options.ReplenishmentPeriod != options.ReplenishmentPeriod
               || Options.AutoReplenishment != options.AutoReplenishment
               || Options.TokensPerPeriod != options.TokensPerPeriod
            ;
    }

    public async Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(TokenBucketRateLimiterOptions options)
    {
        if(CheckOptions(options))
        {
            await ConfigureAsync(options);
        }

        return await AcquireAsync();
    }

    public async Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(int permitCount, TokenBucketRateLimiterOptions options)
    {
        if(CheckOptions(options))
        {
            await ConfigureAsync(options);
        }

        return await AcquireAsync(permitCount);
    }
}