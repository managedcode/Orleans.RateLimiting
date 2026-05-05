using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManagedCode.Orleans.RateLimiting.Core.Models.Holders;

public class GroupLimiterHolder : IAsyncDisposable, IDisposable
{
    private readonly List<LimiterEntry> _holders = [];
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

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    public bool AddLimiter(ILimiterHolder? holder)
    {
        if (holder is not null)
        {
            _holders.Add(new LimiterEntry(holder));
            return true;
        }

        return false;
    }

    public async Task<OrleansRateLimitLease?> AcquireAsync()
    {
        for (var index = 0; index < _holders.Count; index++)
        {
            var entry = _holders[index];
            var lease = await entry.Holder.AcquireAndConfigureAsync();
            if (lease.IsAcquired)
            {
                _holders[index] = entry with { Lease = lease };
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
        await Task.WhenAll(_holders.Select(entry => entry.Lease).Where(lease => lease is not null).Select(lease => lease!.DisposeAsync().AsTask()));
    }

    private readonly record struct LimiterEntry(ILimiterHolder Holder, OrleansRateLimitLease? Lease = null);
}
