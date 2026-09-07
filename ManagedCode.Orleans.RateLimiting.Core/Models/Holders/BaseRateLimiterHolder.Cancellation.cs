using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Models.Holders;

public abstract partial class BaseRateLimiterHolder<TGrain, TOption>
    where TGrain : IRateLimiterGrainWithConfiguration<TOption>
    where TOption : class
{
    public Task<OrleansRateLimitLease> AcquireAsync(int permitCount, CancellationToken cancellationToken)
    {
        return AcquireCancellableAsync(() => _grain.AsReference<ICancellableRateLimiterGrain>().AcquireAsync(permitCount, cancellationToken), cancellationToken);
    }

    public Task<OrleansRateLimitLease> AcquireAndConfigureAsync(int permitCount, CancellationToken cancellationToken)
    {
        return AcquireAndCheckConfigurationAsync(permitCount, _option, cancellationToken);
    }

    public Task<OrleansRateLimitLease> AcquireAndCheckConfigurationAsync(int permitCount, TOption? options, CancellationToken cancellationToken)
    {
        options = _option ?? options;
        return options is null
            ? AcquireAsync(permitCount, cancellationToken)
            : AcquireCancellableAsync(() => _grain.AsReference<ICancellableRateLimiterGrain<TOption>>()
                .AcquireAndCheckConfigurationAsync(permitCount, options, cancellationToken), cancellationToken);
    }

    private async Task<OrleansRateLimitLease> AcquireCancellableAsync(Func<Task<RateLimitLeaseMetadata>> acquire, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var lease = new OrleansRateLimitLease(await acquire(), _grainFactory);
            if (cancellationToken.IsCancellationRequested)
            {
                await lease.DisposeAsync();
                cancellationToken.ThrowIfCancellationRequested();
            }
            return lease;
        }
        catch (TimeoutException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new OrleansRateLimitLease(new RateLimitLeaseMetadata(_grain.GetGrainId()), _grainFactory);
        }
    }
}
