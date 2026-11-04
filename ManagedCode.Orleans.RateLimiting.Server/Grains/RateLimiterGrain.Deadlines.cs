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
        var deadline = new AcquisitionDeadline(timeout, Clock);
        await EnterAcquireAsync(deadline, cancellationToken);
        try { return await AcquireAndPersistAsync(permitCount, deadline, cancellationToken); }
        finally { ExitAcquire(); }
    }

    protected Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationWithDeadlineAsync(int permitCount, TOptions options,
        Func<TOptions, bool> optionsChanged, TimeSpan timeout, CancellationToken cancellationToken)
        => AcquireConfiguredAsync(permitCount, options, optionsChanged, new AcquisitionDeadline(timeout, Clock), cancellationToken);

    private Task WaitForConfigurationLockAsync(AcquisitionDeadline deadline, CancellationToken cancellationToken)
    {
        if (!deadline.IsEnabled)
            return _configurationLock.WaitAsync(cancellationToken);

        var immediate = _configurationLock.WaitAsync(TimeSpan.Zero, cancellationToken);
        return immediate.IsCompletedSuccessfully && immediate.Result
            ? Task.CompletedTask
            : WaitForConfigurationLockWithinDeadlineAsync(deadline, cancellationToken);
    }

    private async Task WaitForConfigurationLockWithinDeadlineAsync(AcquisitionDeadline deadline, CancellationToken cancellationToken)
    {
        using var cancellation = deadline.CreateCancellation(cancellationToken);
        try
        {
            await _configurationLock.WaitAsync(cancellation.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && cancellation.HasTimedOut)
        {
            throw AcquisitionDeadline.CreateException();
        }
    }

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
        using var cancellation = deadline.CreateCancellation(cancellationToken);
        try { return await RateLimiter.AcquireAsync(permitCount, cancellation.Token); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && cancellation.HasTimedOut)
        {
            throw AcquisitionDeadline.CreateException();
        }
    }

    private async Task ConfigureWithinDeadlineAsync(TOptions options, int permitCount, AcquisitionDeadline deadline, CancellationToken cancellationToken)
    {
        using var cancellation = deadline.CreateCancellation(cancellationToken);
        try
        {
            await WaitForActiveAcquiresAsync(cancellation.Token);
            await ConfigureLimiterAsync(options, permitCount, cancellation.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && cancellation.HasTimedOut)
        {
            throw AcquisitionDeadline.CreateException();
        }
    }
}
