using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;

namespace ManagedCode.Orleans.RateLimiting.Core.Interfaces;

public interface IRateLimitRequestPolicy
{
    ValueTask<IReadOnlyList<RateLimitRequestRule>> GetRulesAsync(RateLimitRequestContext context, CancellationToken cancellationToken = default);
}
