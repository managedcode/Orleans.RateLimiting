using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Attributes;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Server.GrainCallFilter;

public class TokenBucketRateLimiterIncomingFilter : BaseRateLimitingIncomingFilter<TokenBucketRateLimiterAttribute, TokenBucketRateLimiterOptions>
{
    public TokenBucketRateLimiterIncomingFilter(IGrainFactory grainFactory) : base(grainFactory)
    {
    }

    protected override ILimiterHolderWithConfiguration<TokenBucketRateLimiterOptions> GetLimiter(string key)
    {
        return GrainFactory.GetTokenBucketRateLimiter(key);
    }
}