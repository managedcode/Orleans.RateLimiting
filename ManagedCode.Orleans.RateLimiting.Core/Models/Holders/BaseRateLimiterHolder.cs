using System;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Models.Holders;

public abstract partial class BaseRateLimiterHolder<TGrain, TOption> : ILimiterHolderWithConfiguration<TOption>, ICancellableLimiterHolder
    where TGrain : IRateLimiterGrainWithConfiguration<TOption>
    where TOption : class
{
    private const int DefaultPermitCount = 1;
    private readonly TGrain _grain;
    private readonly IBoundedRateLimiterGrain<TOption> _boundedGrain;
    private readonly IGrainFactory _grainFactory;
    private readonly TOption? _option;

    internal BaseRateLimiterHolder(TGrain grain, IGrainFactory grainFactory)
    {
        _grain = grain;
        _boundedGrain = grain.AsReference<IBoundedRateLimiterGrain<TOption>>();
        _grainFactory = grainFactory;
    }

    internal BaseRateLimiterHolder(TGrain grain, IGrainFactory grainFactory, TOption option)
    {
        _grain = grain;
        _boundedGrain = grain.AsReference<IBoundedRateLimiterGrain<TOption>>();
        _grainFactory = grainFactory;
        _option = option;
    }

    public Task<OrleansRateLimitLease> AcquireAsync(int permitCount = 1)
        => AcquireAsync(permitCount, System.Threading.CancellationToken.None);

    public Task<OrleansRateLimitLease> AcquireAndConfigureAsync(int permitCount = 1)
        => AcquireAndConfigureAsync(permitCount, System.Threading.CancellationToken.None);

    public ValueTask<RateLimiterStatistics?> GetStatisticsAsync()
    {
        return _grain.GetStatisticsAsync();
    }

    public ValueTask ResetAsync()
    {
        return _grain.ResetAsync();
    }

    public ValueTask DeleteStateAsync()
    {
        return _grain.DeleteStateAsync();
    }

    public Task<OrleansRateLimitLease> AcquireAndCheckConfigurationAsync(TOption? options)
        => AcquireAndCheckConfigurationAsync(DefaultPermitCount, options, System.Threading.CancellationToken.None);

    public Task<OrleansRateLimitLease> AcquireAndCheckConfigurationAsync(int permitCount, TOption? options)
        => AcquireAndCheckConfigurationAsync(permitCount, options, System.Threading.CancellationToken.None);

    public ValueTask Configure(TOption options)
    {
        return _grain.ConfigureAsync(options);
    }

    public ValueTask<TOption> GetConfiguration()
    {
        return _grain.GetConfiguration();
    }
}
