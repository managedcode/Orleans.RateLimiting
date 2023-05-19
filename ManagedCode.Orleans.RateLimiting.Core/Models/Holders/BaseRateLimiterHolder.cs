using System;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Models.Holders;

public abstract class BaseRateLimiterHolder<TGrain, TOption> : ILimiterHolderWithConfiguration<TOption>  
    where TGrain : IRateLimiterGrainWithConfiguration<TOption>
{
    private readonly IGrainFactory _grainFactory;
    private readonly TOption _option;
    private readonly TGrain _grain;

    internal BaseRateLimiterHolder(TGrain grain, IGrainFactory grainFactory)
    {
        _grain = grain;
        _grainFactory = grainFactory;
    }
    
    internal BaseRateLimiterHolder(TGrain grain, IGrainFactory grainFactory, TOption option)
    {
        _grain = grain;
        _grainFactory = grainFactory;
        _option = option;
    }

    public async Task<OrleansRateLimitLease> AcquireAsync(int permitCount = 1)
    {
        try
        {
            var metadata = await _grain.AcquireAsync(permitCount);
            return new OrleansRateLimitLease(metadata, _grainFactory);
        }
        catch (TimeoutException timeoutException)
        {
            return new OrleansRateLimitLease(new RateLimitLeaseMetadata(_grain.GetGrainId()), _grainFactory);
        }
    }

    public async Task<OrleansRateLimitLease> AcquireAndConfigureAsync(int permitCount = 1)
    {
        try
        {
            var metadata = await _grain.AcquireAndCheckConfigurationAsync(_option);
            return new OrleansRateLimitLease(metadata, _grainFactory);
        }
        catch (TimeoutException timeoutException)
        {
            return new OrleansRateLimitLease(new RateLimitLeaseMetadata(_grain.GetGrainId()), _grainFactory);
        }
    }

    public ValueTask<RateLimiterStatistics?> GetStatisticsAsync()
    {
        return _grain.GetStatisticsAsync();
    }

    public ValueTask Configure(TOption options)
    {
        return _grain.ConfigureAsync(options);
    }
    
    public ValueTask<TOption> GetConfiguration()
    {
        return _grain.GetConfiguration();
    }
    
    public async Task<OrleansRateLimitLease> AcquireAndCheckConfigurationAsync(TOption options)
    {
        if (options is null && _option is null)
            return await AcquireAsync();
        
        if(_option is not null)
            options = _option;
        
        try
        {
            var metadata = await _grain.AcquireAndCheckConfigurationAsync(options);
            return new OrleansRateLimitLease(metadata, _grainFactory);
        }
        catch (TimeoutException timeoutException)
        {
            return new OrleansRateLimitLease(new RateLimitLeaseMetadata(_grain.GetGrainId()), _grainFactory);
        }
    }

    public async Task<OrleansRateLimitLease> AcquireAndCheckConfigurationAsync(int permitCount, TOption options)
    {
        if (options is null)
            return await AcquireAsync();
        
        if(_option is not null)
            options = _option;
        
        try
        {
            var metadata = await _grain.AcquireAndCheckConfigurationAsync(permitCount, options);
            return new OrleansRateLimitLease(metadata, _grainFactory);
        }
        catch (TimeoutException timeoutException)
        {
            return new OrleansRateLimitLease(new RateLimitLeaseMetadata(_grain.GetGrainId()), _grainFactory);
        }
    }
}