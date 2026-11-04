using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Orleans.RateLimiting.Core.Models.Holders;

public class GroupLimiterHolder : IAsyncDisposable
{
    private const int DefaultPermitCount = 1;
    private const string AlreadyAcquiredMessage = "A limiter group can only be acquired once before it is disposed.";

    private readonly List<LimiterEntry> _holders = [];
    private readonly object _lifecycleSync = new();
    private Task? _acquisitionSettled;
    private Task? _disposal;
    private bool _acquired;
    private bool _disposed;

    public int Count
    {
        get { lock (_lifecycleSync) return _holders.Count; }
    }

    public async ValueTask DisposeAsync()
    {
        TaskCompletionSource? completion = null;
        Task? acquisition = null;
        Task disposal;
        lock (_lifecycleSync)
        {
            _disposed = true;
            if (_disposal is null)
            {
                acquisition = _acquisitionSettled;
                // A settled rollback has already released every lease.
                if (acquisition is null || (!_acquired && acquisition.IsCompletedSuccessfully))
                    _disposal = Task.CompletedTask;
                else
                {
                    completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    _disposal = completion.Task;
                }
            }
            disposal = _disposal;
        }

        if (completion is not null)
        {
            var work = DisposeCoreAsync(acquisition);
            await work.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            completion.SetFromTask(work);
        }

        await disposal;
        GC.SuppressFinalize(this);
    }

    public bool AddLimiter(ILimiterHolder? holder)
    {
        lock (_lifecycleSync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_acquired)
                throw new InvalidOperationException(AlreadyAcquiredMessage);
            if (holder is null)
                return false;

            _holders.Add(new LimiterEntry(holder));
            return true;
        }
    }

    public Task<OrleansRateLimitLease?> AcquireAsync() => AcquireAsync(CancellationToken.None);

    public async Task<OrleansRateLimitLease?> AcquireAsync(CancellationToken cancellationToken)
    {
        var settlement = ReserveAcquisition(cancellationToken);
        if (settlement is null)
            return null;

        try
        {
            for (var index = 0; index < _holders.Count; index++)
            {
                var entry = _holders[index];
                cancellationToken.ThrowIfCancellationRequested();
                var lease = cancellationToken.CanBeCanceled && entry.Holder is ICancellableLimiterHolder cancellable
                    ? await cancellable.AcquireAndConfigureAsync(DefaultPermitCount, cancellationToken)
                    : await entry.Holder.AcquireAndConfigureAsync();
                // Retain ownership before checking cancellation, including legacy holders.
                _holders[index] = entry with { Lease = lease };
                cancellationToken.ThrowIfCancellationRequested();
                if (!lease.IsAcquired)
                {
                    await ReleaseAcquiredAsync();
                    return lease;
                }
            }
            cancellationToken.ThrowIfCancellationRequested();
            return null;
        }
        catch
        {
            await ReleaseAcquiredAsync();
            throw;
        }
        finally
        {
            settlement.SetResult();
        }
    }

    private TaskCompletionSource? ReserveAcquisition(CancellationToken cancellationToken)
    {
        lock (_lifecycleSync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_acquired)
                throw new InvalidOperationException(AlreadyAcquiredMessage);

            if (_holders.Count == default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _acquired = true;
                return null;
            }

            // A holder can request disposal synchronously when invoked.
            var settlement = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _acquired = true;
            _acquisitionSettled = settlement.Task;
            return settlement;
        }
    }

    private async Task DisposeCoreAsync(Task? acquisition)
    {
        if (acquisition is not null)
            await acquisition;
        await ReleaseAcquiredAsync();
    }

    private async Task ReleaseAcquiredAsync()
    {
        List<Task>? pending = null;
        for (var index = 0; index < _holders.Count; index++)
        {
            var entry = _holders[index];
            if (entry.Lease is not { } lease)
                continue;

            _holders[index] = entry with { Lease = null };
            var release = lease.DisposeAsync().AsTask();
            if (!release.IsCompletedSuccessfully)
                (pending ??= []).Add(release);
        }

        if (pending is not null)
            await Task.WhenAll(pending);
        lock (_lifecycleSync)
            _acquired = false;
    }

    private readonly record struct LimiterEntry(ILimiterHolder Holder, OrleansRateLimitLease? Lease = null);
}
