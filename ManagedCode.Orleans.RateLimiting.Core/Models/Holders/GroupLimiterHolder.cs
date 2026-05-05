using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManagedCode.Orleans.RateLimiting.Core.Models.Holders;

public class GroupLimiterHolder : IAsyncDisposable
{
    private const string AlreadyAcquiredMessage = "A limiter group can only be acquired once before it is disposed.";

    private readonly List<LimiterEntry> _holders = [];
    private bool _acquired;
    private bool _disposed;

    public int Count => _holders.Count;

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        await ReleaseAcquiredAsync();
        GC.SuppressFinalize(this);
    }

    public bool AddLimiter(ILimiterHolder? holder)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (holder is not null)
        {
            _holders.Add(new LimiterEntry(holder));
            return true;
        }

        return false;
    }

    public async Task<OrleansRateLimitLease?> AcquireAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_acquired)
            throw new InvalidOperationException(AlreadyAcquiredMessage);

        for (var index = 0; index < _holders.Count; index++)
        {
            var entry = _holders[index];
            var lease = await entry.Holder.AcquireAndConfigureAsync();
            if (lease.IsAcquired)
            {
                _holders[index] = entry with { Lease = lease };
                _acquired = true;
            }
            else
            {
                await ReleaseAcquiredAsync();
                return lease;
            }
        }

        return null;
    }

    private async Task ReleaseAcquiredAsync()
    {
        var leases = _holders.Select(entry => entry.Lease).Where(lease => lease is not null).ToArray();

        for (var index = 0; index < _holders.Count; index++)
            _holders[index] = _holders[index] with { Lease = null };

        _acquired = false;

        await Task.WhenAll(leases.Select(lease => lease!.DisposeAsync().AsTask()));
    }

    private readonly record struct LimiterEntry(ILimiterHolder Holder, OrleansRateLimitLease? Lease = null);
}
