using System;
using System.Collections.Generic;
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
        var policyName = NormalizePolicyName(context.PolicyName);
        var configuredRules = _options.Value.Rules;
        List<RateLimitRequestRule>? matches = null;
        for (var index = default(int); index < configuredRules.Count; index++)
        {
            var rule = configuredRules[index];
            if (NormalizePolicyName(rule.PolicyName) == policyName)
                (matches ??= []).Add(rule);
        }
        return ValueTask.FromResult<IReadOnlyList<RateLimitRequestRule>>(matches is null ? Array.Empty<RateLimitRequestRule>() : matches);
    }

    private static string NormalizePolicyName(string? policyName)
    {
        return string.IsNullOrWhiteSpace(policyName) ? string.Empty : policyName.Trim().ToUpperInvariant();
    }
}
