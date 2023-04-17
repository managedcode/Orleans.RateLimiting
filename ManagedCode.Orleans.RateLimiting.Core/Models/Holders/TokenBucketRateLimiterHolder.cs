using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Models.Holders;

public class TokenBucketRateLimiterHolder : BaseRateLimiterHolder<ITokenBucketRateLimiterGrain, TokenBucketRateLimiterOptions>
{
    public TokenBucketRateLimiterHolder(ITokenBucketRateLimiterGrain grain, IGrainFactory grainFactory) : base(grain,
        grainFactory)
    {
    }
    
}