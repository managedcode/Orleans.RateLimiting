using System;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Models;

namespace ManagedCode.Orleans.RateLimiting.Server.Grains;

public abstract partial class RateLimiterGrain<TLimiter, TOptions>
    where TLimiter : RateLimiter
    where TOptions : class
{
    protected virtual bool IsQueueEnabled => true;

    public Task<RateLimitLeaseMetadata> AcquireWithDeadlineAsync(int permitCount, TimeSpan timeout)
        => AcquireCancellableWithDeadlineAsync(permitCount, timeout, CancellationToken.None);

    public async Task<RateLimitLeaseMetadata> AcquireCancellableWithDeadlineAsync(int permitCount, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var deadline = new AcquisitionDeadline(timeout);
        await EnterAcquireAsync(deadline, cancellationToken);
        try { return await AcquireAndPersistAsync(permitCount, deadline, cancellationToken); }
        finally { ExitAcquire(); }
    }

    protected Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationWithDeadlineAsync(int permitCount, TOptions options,
        Func<TOptions, bool> optionsChanged, TimeSpan timeout, CancellationToken cancellationToken)
        => AcquireConfiguredAsync(permitCount, options, optionsChanged, new AcquisitionDeadline(timeout), cancellationToken);

    private ValueTask<RateLimitLease> AcquireNativeAsync(int permitCount, AcquisitionDeadline deadline, CancellationToken cancellationToken)
    {
        if (!deadline.IsEnabled) return RateLimiter.AcquireAsync(permitCount, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        deadline.ThrowIfExpired();
        var lease = RateLimiter.AttemptAcquire(permitCount);
        if (lease.IsAcquired || !IsQueueEnabled) return new(lease);
        lease.Dispose();
        return AcquireQueuedNativeAsync(permitCount, deadline, cancellationToken);
    }

    private async ValueTask<RateLimitLease> AcquireQueuedNativeAsync(int permitCount, AcquisitionDeadline deadline, CancellationToken cancellationToken)
    {
        using var cancellation = deadline.CreateCancellationSource(cancellationToken);
        try { return await RateLimiter.AcquireAsync(permitCount, cancellation.Token); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && cancellation.IsCancellationRequested)
        {
            throw AcquisitionDeadline.CreateException();
        }
    }

    private async Task ConfigureWithinDeadlineAsync(TOptions options, int permitCount, AcquisitionDeadline deadline, CancellationToken cancellationToken)
    {
        using var cancellation = deadline.IsEnabled ? deadline.CreateCancellationSource(cancellationToken) : null;
        var token = cancellation?.Token ?? cancellationToken;
        try
        {
            await WaitForActiveAcquiresAsync(token);
            await ConfigureLimiterAsync(options, permitCount, token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && cancellation?.IsCancellationRequested == true)
        {
            throw AcquisitionDeadline.CreateException();
        }
    }
}
