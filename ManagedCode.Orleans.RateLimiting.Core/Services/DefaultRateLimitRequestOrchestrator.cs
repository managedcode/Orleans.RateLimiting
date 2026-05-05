using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Exceptions;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;
using ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Services;

public sealed class DefaultRateLimitRequestOrchestrator : IRateLimitRequestOrchestrator
{
    private readonly IEnumerable<RateLimiterConfig> _configs;
    private readonly IGrainFactory _grainFactory;
    private readonly IRateLimitRequestKeyResolver _keyResolver;
    private readonly IEnumerable<IRateLimitRequestPolicy> _policies;

    public DefaultRateLimitRequestOrchestrator(
        IGrainFactory grainFactory,
        IEnumerable<RateLimiterConfig> configs,
        IEnumerable<IRateLimitRequestPolicy> policies,
        IRateLimitRequestKeyResolver keyResolver)
    {
        _grainFactory = grainFactory;
        _configs = configs;
        _policies = policies;
        _keyResolver = keyResolver;
    }

    public async ValueTask<GroupLimiterHolder> CreateLimiterGroupAsync(RateLimitRequestContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var holder = new GroupLimiterHolder();

        foreach (var policy in _policies)
        {
            var rules = await policy.GetRulesAsync(context, cancellationToken);
            foreach (var rule in rules)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await AddLimiterAsync(holder, context, rule, cancellationToken);
            }
        }

        return holder;
    }

    private async ValueTask AddLimiterAsync(GroupLimiterHolder holder, RateLimitRequestContext context, RateLimitRequestRule rule, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rule.ConfigurationName))
            throw new RateLimitConfigurationNotFoundException(rule.ConfigurationName);

        var key = await _keyResolver.ResolveKeyAsync(context, rule, cancellationToken);
        if (string.IsNullOrWhiteSpace(key))
        {
            if (rule.Required)
                throw new RateLimitPartitionKeyNotFoundException(rule.Kind);

            return;
        }

        var partition = new RateLimitRequestPartition(rule.Kind, key, rule.ConfigurationName);
        var limiter = _grainFactory.GetRateLimiterByConfig(partition.ToString(), rule.ConfigurationName, _configs);

        if (limiter is null)
            throw new RateLimitConfigurationNotFoundException(rule.ConfigurationName);

        holder.AddLimiter(limiter);
    }
}
