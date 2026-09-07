using System;
using System.Diagnostics;
using ManagedCode.Orleans.RateLimiting.Server.Diagnostics;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace ManagedCode.Orleans.RateLimiting.Server.Grains;

public abstract partial class RateLimiterGrain<TLimiter, TOptions>
    where TLimiter : RateLimiter
    where TOptions : class
{
    private void AddActiveLeaseState(RateLimiterGrainState<TOptions> state, Guid leaseId, int permitCount)
    {
        if (!TracksActiveLeaseState)
            return;

        RemoveActiveLeaseState(state, leaseId);
        state.ActiveLeases.Add(new RateLimiterLeaseState(leaseId, permitCount));
    }

    private void CaptureRuntimeSnapshot(RateLimiterGrainState<TOptions> state)
    {
        var runtimeStatistics = RateLimiter.GetStatistics();
        state.HasSnapshot = true;
        state.CurrentAvailablePermits = ToAvailablePermitCount(runtimeStatistics?.CurrentAvailablePermits);
        state.UpdatedAtUtc = Clock.GetUtcNow();
    }

    private async Task ClearStoredStateAsync()
    {
        await _stateLock.WaitAsync();
        try
        {
            await _state.ClearStateAsync();
            _state.State = new RateLimiterGrainState<TOptions>();
            _stateDirty = false;
            _stateDeleted = true;
        }
        finally
        {
            _stateLock.Release();
        }
    }

    private void DisposeStateFlushTimer()
    {
        _stateFlushTimer?.Dispose();
        _stateFlushTimer = null;
    }

    private async Task FlushStateTimerAsync(CancellationToken cancellationToken)
    {
        try
        {
            await FlushStateIfDirtyAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected when the timer or activation is stopping. Dirty state is retained.
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, RateLimiterLogMessages.StateFlushFailed, typeof(TLimiter).Name, this.GetPrimaryKeyString());
        }
    }

    private async Task FlushStateIfDirtyAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        await _stateLock.WaitAsync(cancellationToken);
        try
        {
            await FlushStateIfDirtyLockedAsync(cancellationToken);
        }
        finally
        {
            _stateLock.Release();
        }
    }

    private async Task FlushStateIfDirtyLockedAsync(CancellationToken cancellationToken)
    {
        if (!_stateDirty)
            return;

        var started = Stopwatch.GetTimestamp();
        var outcome = RateLimiterMetrics.Failed;
        try
        {
            await _state.WriteStateAsync(cancellationToken);
            _stateDirty = false;
            outcome = RateLimiterMetrics.Written;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            outcome = RateLimiterMetrics.Cancelled;
            throw;
        }
        finally
        {
            RateLimiterMetrics.RecordStateWrite<TLimiter>(outcome, started);
        }
    }

    private int GetRestoredAvailablePermits()
    {
        var state = _state.State;
        if (!state.HasSnapshot)
            return PermitLimit;

        return GetRestoredAvailablePermits(state.UpdatedAtUtc, state.CurrentAvailablePermits, Clock.GetUtcNow());
    }

    private async Task RestoreRateLimiterAsync()
    {
        if (!_state.State.HasSnapshot)
            return;

        if (TracksActiveLeaseState)
            RestoreActiveLeases();
        else
            RestoreConsumedPermits();

        await MutateStateAsync(CaptureRuntimeSnapshot);
    }

    private void RestoreActiveLeases()
    {
        foreach (var activeLease in _state.State.ActiveLeases)
        {
            var lease = RateLimiter.AttemptAcquire(activeLease.PermitCount);
            if (lease.IsAcquired)
                TrackLease(activeLease.LeaseId, lease, activeLease.PermitCount);
            else
                lease.Dispose();
        }
    }

    private void RestoreConsumedPermits()
    {
        var restoredAvailablePermits = GetRestoredAvailablePermits();
        var consumedPermits = PermitLimit - restoredAvailablePermits;
        if (consumedPermits <= NoAvailablePermits)
            return;

        RateLimiter.AttemptAcquire(consumedPermits).Dispose();
    }

    private static void RemoveActiveLeaseState(RateLimiterGrainState<TOptions> state, Guid leaseId)
    {
        state.ActiveLeases.RemoveAll(lease => lease.LeaseId == leaseId);
    }

    private void ResetQuotaState(RateLimiterGrainState<TOptions> state)
    {
        state.ActiveLeases.Clear();
        state.CurrentAvailablePermits = PermitLimit;
        state.HasSnapshot = true;
        state.TotalFailedLeases = NoAvailablePermits;
        state.TotalSuccessfulLeases = NoAvailablePermits;
        state.UpdatedAtUtc = Clock.GetUtcNow();
    }

    private void ResetStateForConfiguration(RateLimiterGrainState<TOptions> state, TOptions options)
    {
        ResetQuotaState(state);
        state.HasConfiguration = true;
        state.Options = options;
    }

    private int ToAvailablePermitCount(long? availablePermits)
    {
        return ClampAvailablePermits((int)(availablePermits ?? PermitLimit));
    }

    private async Task MutateStateAsync(Action<RateLimiterGrainState<TOptions>> update, bool flushImmediately = false, CancellationToken cancellationToken = default, bool cancelBeforeMutation = false)
    {
        // Runtime mutation and its dirty snapshot must remain atomic on cancellation.
        await _stateLock.WaitAsync(cancelBeforeMutation ? cancellationToken : CancellationToken.None);
        try
        {
            update(_state.State);
            _stateDeleted = false;
            _stateDirty = true;

            if (flushImmediately)
                await FlushStateIfDirtyLockedAsync(cancellationToken);
        }
        finally
        {
            _stateLock.Release();
        }
    }

    private void RegisterStateFlushTimer()
    {
        if (_stateFlushPeriod <= TimeSpan.Zero)
            return;

        _stateFlushTimer = this.RegisterGrainTimer(
            FlushStateTimerAsync,
            new GrainTimerCreationOptions(_stateFlushPeriod, _stateFlushPeriod)
            {
                Interleave = true,
                KeepAlive = false
            });
    }
}
