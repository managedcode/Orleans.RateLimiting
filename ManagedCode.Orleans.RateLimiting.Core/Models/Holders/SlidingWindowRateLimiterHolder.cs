using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Models.Holders;

public class SlidingWindowRateLimiterHolder : BaseRateLimiterHolder<ISlidingWindowRateLimiterGrain, SlidingWindowRateLimiterOptions>
{
    public SlidingWindowRateLimiterHolder(ISlidingWindowRateLimiterGrain grain, IGrainFactory grainFactory) : base(grain, grainFactory)
    {
    }

    public SlidingWindowRateLimiterHolder(ISlidingWindowRateLimiterGrain grain, IGrainFactory grainFactory, SlidingWindowRateLimiterOptions options) : base(grain,
        grainFactory, options)
    {
    }
}