using System;
using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Server.Diagnostics;

namespace ManagedCode.Orleans.RateLimiting.Server.Grains;

public abstract partial class RateLimiterGrain<TLimiter, TOptions>
    where TLimiter : RateLimiter
    where TOptions : class
{
    private void TrackLease(Guid leaseId, RateLimitLease lease, int permitCount)
    {
        if (!_rateLimitLeases.TryAdd(leaseId, new TrackedLease(lease, permitCount)))
        {
            lease.Dispose();
            return;
        }
        if (TracksActiveLeaseState)
            RateLimiterMetrics.ChangeActivePermits(permitCount);
    }

    private void DisposeTrackedLease(TrackedLease lease)
    {
        lease.Lease.Dispose();
        if (TracksActiveLeaseState)
            RateLimiterMetrics.ChangeActivePermits(-lease.PermitCount);
    }

    private readonly record struct TrackedLease(RateLimitLease Lease, int PermitCount);
}
