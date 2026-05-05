using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;
using ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;

namespace ManagedCode.Orleans.RateLimiting.Core.Interfaces;

public interface IRateLimitRequestOrchestrator
{
    ValueTask<GroupLimiterHolder> CreateLimiterGroupAsync(RateLimitRequestContext context, CancellationToken cancellationToken = default);
}
