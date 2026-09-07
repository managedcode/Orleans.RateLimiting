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
        return AcquireCancellableAsync(permitCount, null, cancellationToken);
    }

    public Task<OrleansRateLimitLease> AcquireAndConfigureAsync(int permitCount, CancellationToken cancellationToken)
    {
        return AcquireAndCheckConfigurationAsync(permitCount, _option, cancellationToken);
    }

    public Task<OrleansRateLimitLease> AcquireAndCheckConfigurationAsync(int permitCount, TOption? options, CancellationToken cancellationToken)
    {
        options = _option ?? options;
        return AcquireCancellableAsync(permitCount, options, cancellationToken);
    }

    private async Task<OrleansRateLimitLease> AcquireCancellableAsync(int permitCount, TOption? options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var metadata = options is null
                ? await AcquireBoundedAsync(permitCount, cancellationToken)
                : await AcquireConfiguredBoundedAsync(permitCount, options, cancellationToken);
            var lease = new OrleansRateLimitLease(metadata, _grainFactory, _grain);
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

    private Task<RateLimitLeaseMetadata> AcquireBoundedAsync(int permitCount, CancellationToken cancellationToken)
    {
        IBoundedRateLimiterGrain grain = _boundedGrain;
        return cancellationToken.CanBeCanceled
            ? grain.AcquireCancellableWithDeadlineAsync(permitCount, Timeout.InfiniteTimeSpan, cancellationToken)
            : grain.AcquireWithDeadlineAsync(permitCount, Timeout.InfiniteTimeSpan);
    }

    private Task<RateLimitLeaseMetadata> AcquireConfiguredBoundedAsync(int permitCount, TOption options, CancellationToken cancellationToken)
    {
        var grain = _boundedGrain;
        return cancellationToken.CanBeCanceled
            ? grain.AcquireAndCheckConfigurationCancellableWithDeadlineAsync(permitCount, options, Timeout.InfiniteTimeSpan, cancellationToken)
            : grain.AcquireAndCheckConfigurationWithDeadlineAsync(permitCount, options, Timeout.InfiniteTimeSpan);
    }

}
