using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManagedCode.Orleans.RateLimiting.Core.Models.Holders;

public class GroupLimiterHolder : IAsyncDisposable
{
    private const string AlreadyAcquiredMessage = "A limiter group can only be acquired once before it is disposed.";

    private readonly List<LimiterEntry> _holders = [];
    private readonly object _lifecycleSync = new();
    private Task<OrleansRateLimitLease?>? _acquisition;
    private Task? _disposal;
    private bool _acquired;
    private bool _disposed;

    public int Count
    {
        get { lock (_lifecycleSync) return _holders.Count; }
    }

    public async ValueTask DisposeAsync()
    {
        Task disposal;
        lock (_lifecycleSync)
        {
            _disposed = true;
            disposal = _disposal ??= DisposeCoreAsync(_acquisition);
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

    public async Task<OrleansRateLimitLease?> AcquireAsync()
    {
        Task<OrleansRateLimitLease?> acquisition;
        lock (_lifecycleSync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_acquired)
                throw new InvalidOperationException(AlreadyAcquiredMessage);

            // Reserve the group before the first asynchronous grain response can arrive.
            _acquired = true;
            acquisition = _acquisition = AcquireCoreAsync();
        }
        return await acquisition;
    }

    private async Task<OrleansRateLimitLease?> AcquireCoreAsync()
    {
        try
        {
            for (var index = 0; index < _holders.Count; index++)
            {
                var entry = _holders[index];
                var lease = await entry.Holder.AcquireAndConfigureAsync();
                if (!lease.IsAcquired)
                {
                    await ReleaseAcquiredAsync();
                    return lease;
                }
                _holders[index] = entry with { Lease = lease };
            }
            return null;
        }
        catch
        {
            await ReleaseAcquiredAsync();
            throw;
        }
    }

    private async Task DisposeCoreAsync(Task<OrleansRateLimitLease?>? acquisition)
    {
        try
        {
            if (acquisition is not null)
                await acquisition;
        }
        catch
        {
            // AcquireAsync delivers the original failure; disposal must still release leases.
        }
        await ReleaseAcquiredAsync();
    }

    private async Task ReleaseAcquiredAsync()
    {
        var leases = _holders.Select(entry => entry.Lease).Where(lease => lease is not null).ToArray();
        for (var index = 0; index < _holders.Count; index++)
            _holders[index] = _holders[index] with { Lease = null };

        await Task.WhenAll(leases.Select(lease => lease!.DisposeAsync().AsTask()));
        lock (_lifecycleSync)
            _acquired = false;
    }

    private readonly record struct LimiterEntry(ILimiterHolder Holder, OrleansRateLimitLease? Lease = null);
}
