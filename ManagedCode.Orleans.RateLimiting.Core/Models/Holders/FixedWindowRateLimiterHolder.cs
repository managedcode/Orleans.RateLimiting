using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Models.Holders;

public class FixedWindowRateLimiterHolder : BaseRateLimiterHolder<IFixedWindowRateLimiterGrain, FixedWindowRateLimiterOptions>
{
    public FixedWindowRateLimiterHolder(IFixedWindowRateLimiterGrain grain, IGrainFactory grainFactory) : base(grain, grainFactory)
    {
    }

    public FixedWindowRateLimiterHolder(IFixedWindowRateLimiterGrain grain, IGrainFactory grainFactory, FixedWindowRateLimiterOptions options) : base(grain, grainFactory,
        options)
    {
    }
}