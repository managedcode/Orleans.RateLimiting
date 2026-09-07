using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Orleans.RateLimiting.Core.Models.Holders;

/// <summary>Optional capability for cancelling queued acquisitions without changing existing holder implementations.</summary>
public interface ICancellableLimiterHolder : ILimiterHolder
{
    Task<OrleansRateLimitLease> AcquireAsync(int permitCount, CancellationToken cancellationToken);
    Task<OrleansRateLimitLease> AcquireAndConfigureAsync(int permitCount, CancellationToken cancellationToken);
}
