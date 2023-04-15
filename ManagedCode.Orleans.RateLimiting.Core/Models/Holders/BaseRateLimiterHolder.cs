using System.Threading.RateLimiting;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Models.Holders;

public abstract class BaseRateLimiterHolder<TGrain, TOption> where TGrain : IRateLimiterGrain
{
    private readonly IGrainFactory _grainFactory;
    protected readonly TGrain Grain;

    internal BaseRateLimiterHolder(TGrain grain, IGrainFactory grainFactory)
    {
        Grain = grain;
        _grainFactory = grainFactory;
    }

    public abstract ValueTask Configure(TOption options);

    public async ValueTask<OrleansRateLimitLease> AcquireAsync(int permitCount = 1)
    {
        var metadata = await Grain.AcquireAsync(permitCount);
        return new OrleansRateLimitLease(metadata, _grainFactory);
    }

    public ValueTask<RateLimiterStatistics?> GetStatisticsAsync()
    {
        return Grain.GetStatisticsAsync();
    }
}