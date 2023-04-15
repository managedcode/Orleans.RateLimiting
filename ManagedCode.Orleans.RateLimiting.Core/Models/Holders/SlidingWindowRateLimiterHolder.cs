using System.Threading.RateLimiting;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Models.Holders;

public class
    SlidingWindowRateLimiterHolder : BaseRateLimiterHolder<ISlidingWindowRateLimiterGrain,
        SlidingWindowRateLimiterOptions>
{
    public SlidingWindowRateLimiterHolder(ISlidingWindowRateLimiterGrain grain, IGrainFactory grainFactory) : base(
        grain, grainFactory)
    {
    }

    public override ValueTask Configure(SlidingWindowRateLimiterOptions options)
    {
        return Grain.ConfigureAsync(options);
    }
}