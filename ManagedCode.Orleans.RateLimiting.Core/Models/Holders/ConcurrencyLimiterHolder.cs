using System.Threading.RateLimiting;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Models.Holders;

public class ConcurrencyLimiterHolder : BaseRateLimiterHolder<IConcurrencyLimiterGrain, ConcurrencyLimiterOptions>
{
    public ConcurrencyLimiterHolder(IConcurrencyLimiterGrain grain, IGrainFactory grainFactory) : base(grain,
        grainFactory)
    {
    }

    public override ValueTask Configure(ConcurrencyLimiterOptions options)
    {
        return Grain.ConfigureAsync(options);
    }
}