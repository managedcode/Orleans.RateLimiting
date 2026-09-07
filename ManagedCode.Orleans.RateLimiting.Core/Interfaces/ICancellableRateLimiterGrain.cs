using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using Orleans;
using Orleans.Concurrency;

namespace ManagedCode.Orleans.RateLimiting.Core.Interfaces;

/// <summary>Optional cancellation capability. Existing grain contracts retain their RPC identities.</summary>
public interface ICancellableRateLimiterGrain : IGrainWithStringKey
{
    [AlwaysInterleave]
    Task<RateLimitLeaseMetadata> AcquireAsync(int permitCount, CancellationToken cancellationToken);
}

public interface ICancellableRateLimiterGrain<TOptions> : ICancellableRateLimiterGrain
{
    [AlwaysInterleave]
    Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(int permitCount, TOptions options, CancellationToken cancellationToken);
}
