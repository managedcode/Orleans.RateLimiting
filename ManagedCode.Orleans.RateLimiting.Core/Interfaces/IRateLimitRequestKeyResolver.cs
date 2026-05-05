using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;

namespace ManagedCode.Orleans.RateLimiting.Core.Interfaces;

public interface IRateLimitRequestKeyResolver
{
    ValueTask<string?> ResolveKeyAsync(RateLimitRequestContext context, RateLimitPartitionKind partitionKind, CancellationToken cancellationToken = default);
}
