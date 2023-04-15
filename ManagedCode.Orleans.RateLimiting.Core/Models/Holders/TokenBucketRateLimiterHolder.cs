using System.Threading.RateLimiting;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Models.Holders;

public class
    TokenBucketRateLimiterHolder : BaseRateLimiterHolder<ITokenBucketRateLimiterGrain, TokenBucketRateLimiterOptions>
{
    public TokenBucketRateLimiterHolder(ITokenBucketRateLimiterGrain grain, IGrainFactory grainFactory) : base(grain,
        grainFactory)
    {
    }

    public override ValueTask Configure(TokenBucketRateLimiterOptions options)
    {
        return Grain.ConfigureAsync(options);
    }
}