using System;
using System.Diagnostics;
using ManagedCode.Orleans.RateLimiting.Server.Diagnostics;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Server.Grains;

public abstract partial class RateLimiterGrain<TLimiter, TOptions>
    where TLimiter : RateLimiter
    where TOptions : class
{
    protected async Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(TOptions options, Func<TOptions, bool> optionsChanged)
    {
        return await AcquireAndCheckConfigurationAsync(permitCount: 1, options, optionsChanged);
    }

    protected Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(int permitCount, TOptions options, Func<TOptions, bool> optionsChanged)
        => AcquireAndCheckConfigurationAsync(permitCount, options, optionsChanged, CancellationToken.None);

    protected async Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(int permitCount, TOptions options, Func<TOptions, bool> optionsChanged, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(permitCount);
        await EnterAcquireAsync(options, optionsChanged, permitCount, cancellationToken);
        try
        {
            return await AcquireAndPersistAsync(permitCount, cancellationToken);
        }
        finally
        {
            ExitAcquire();
        }
    }

    private async Task<RateLimitLeaseMetadata> AcquireAndPersistAsync(int permitCount, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        var outcome = RateLimiterMetrics.Failed;
        try
        {
            var metadata = await AcquireLeaseCoreAsync(permitCount, cancellationToken);
            outcome = metadata.IsAcquired ? RateLimiterMetrics.Acquired : RateLimiterMetrics.Rejected;
            return metadata;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            outcome = RateLimiterMetrics.Cancelled;
            throw;
        }
        finally
        {
            RateLimiterMetrics.RecordAcquisition<TLimiter>(outcome, started);
        }
    }

    private async Task<RateLimitLeaseMetadata> AcquireLeaseCoreAsync(int permitCount, CancellationToken cancellationToken)
    {
        var leaseId = Guid.NewGuid();
        var lease = await RateLimiter.AcquireAsync(permitCount, cancellationToken);
        if (cancellationToken.IsCancellationRequested)
        {
            lease.Dispose();
            cancellationToken.ThrowIfCancellationRequested();
        }
        var metadata = new RateLimitLeaseMetadata(leaseId, this.GetGrainId(), lease);

        if (!lease.IsAcquired)
            return await PersistRejectedLeaseAsync(lease, metadata);

        await PersistAcquiredLeaseAsync(leaseId, lease, permitCount);
        if (cancellationToken.IsCancellationRequested)
        {
            await ReleaseLease(leaseId);
            cancellationToken.ThrowIfCancellationRequested();
        }
        return metadata;
    }

    private async Task<RateLimitLeaseMetadata> PersistRejectedLeaseAsync(RateLimitLease lease, RateLimitLeaseMetadata metadata)
    {
        lease.Dispose();
        await MutateStateAsync(state =>
        {
            state.TotalFailedLeases++;
            CaptureRuntimeSnapshot(state);
        });

        return metadata;
    }

    private async Task PersistAcquiredLeaseAsync(Guid leaseId, RateLimitLease lease, int permitCount)
    {
        await MutateStateAsync(state =>
        {
            state.TotalSuccessfulLeases++;
            AddActiveLeaseState(state, leaseId, permitCount);
            CaptureRuntimeSnapshot(state);
        });

        TrackLease(leaseId, lease, permitCount);
    }

    private async Task EnterAcquireAsync(CancellationToken cancellationToken)
    {
        await _configurationLock.WaitAsync(cancellationToken);
        try
        {
            lock (_limiterLifetimeSync)
            {
                _activeAcquireCount++;
            }
        }
        finally
        {
            _configurationLock.Release();
        }
    }

    private async Task EnterAcquireAsync(TOptions options, Func<TOptions, bool> optionsChanged, int permitCount, CancellationToken cancellationToken)
    {
        await _configurationLock.WaitAsync(cancellationToken);
        try
        {
            if (optionsChanged(options))
            {
                await WaitForActiveAcquiresAsync(cancellationToken);
                await ConfigureLimiterAsync(options, permitCount, cancellationToken);
            }

            lock (_limiterLifetimeSync)
            {
                _activeAcquireCount++;
            }
        }
        finally
        {
            _configurationLock.Release();
        }
    }

    private void ExitAcquire()
    {
        TaskCompletionSource? completed = null;

        lock (_limiterLifetimeSync)
        {
            _activeAcquireCount--;
            if (_activeAcquireCount == NoActiveAcquires)
            {
                completed = _noActiveAcquires;
                _noActiveAcquires = null;
            }
        }

        completed?.TrySetResult();
    }

    private Task WaitForActiveAcquiresAsync(CancellationToken cancellationToken = default)
    {
        Task waitTask;

        lock (_limiterLifetimeSync)
        {
            if (_activeAcquireCount == NoActiveAcquires)
                return Task.CompletedTask;

            _noActiveAcquires ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            waitTask = _noActiveAcquires.Task;
        }

        return waitTask.WaitAsync(cancellationToken);
    }

}
