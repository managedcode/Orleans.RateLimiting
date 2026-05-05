using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;
using ManagedCode.Orleans.RateLimiting.Core.Options;
using Microsoft.Extensions.Options;

namespace ManagedCode.Orleans.RateLimiting.Core.Services;

public sealed class OptionsRateLimitRequestPolicy : IRateLimitRequestPolicy
{
    private readonly IOptions<RateLimitRequestOrchestrationOptions> _options;

    public OptionsRateLimitRequestPolicy(IOptions<RateLimitRequestOrchestrationOptions> options)
    {
        _options = options;
    }

    public ValueTask<IReadOnlyList<RateLimitRequestRule>> GetRulesAsync(RateLimitRequestContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<IReadOnlyList<RateLimitRequestRule>>(_options.Value.Rules.ToArray());
    }
}
