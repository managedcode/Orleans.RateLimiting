using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using Orleans;
using Orleans.Concurrency;

namespace ManagedCode.Orleans.RateLimiting.Core.Interfaces;

/// <summary>Server-enforced acquisition budgets without cancellation traffic on the fast path.</summary>
public interface IBoundedRateLimiterGrain : IGrainWithStringKey
{
    [AlwaysInterleave]
    Task<RateLimitLeaseMetadata> AcquireWithDeadlineAsync(int permitCount, TimeSpan timeout);

    [AlwaysInterleave]
    Task<RateLimitLeaseMetadata> AcquireCancellableWithDeadlineAsync(int permitCount, TimeSpan timeout, CancellationToken cancellationToken);
}

public interface IBoundedRateLimiterGrain<TOptions> : IBoundedRateLimiterGrain
{
    [AlwaysInterleave]
    Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationWithDeadlineAsync(int permitCount, TOptions options, TimeSpan timeout);

    [AlwaysInterleave]
    Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationCancellableWithDeadlineAsync(int permitCount, TOptions options, TimeSpan timeout, CancellationToken cancellationToken);
}
