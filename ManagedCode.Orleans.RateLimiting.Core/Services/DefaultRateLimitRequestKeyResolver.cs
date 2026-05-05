using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;

namespace ManagedCode.Orleans.RateLimiting.Core.Services;

public sealed class DefaultRateLimitRequestKeyResolver : IRateLimitRequestKeyResolver
{
    public ValueTask<string?> ResolveKeyAsync(RateLimitRequestContext context, RateLimitRequestRule rule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(rule);

        cancellationToken.ThrowIfCancellationRequested();

        var key = rule.Key ?? rule.Kind switch
        {
            RateLimitPartitionKind.User => context.UserId,
            RateLimitPartitionKind.Group => context.GroupId,
            RateLimitPartitionKind.Tenant => context.TenantId,
            RateLimitPartitionKind.Role => context.Role,
            RateLimitPartitionKind.IpAddress => context.IpAddress,
            RateLimitPartitionKind.Endpoint => context.OperationName,
            RateLimitPartitionKind.Grain => context.Resource,
            RateLimitPartitionKind.Custom => ResolveCustomKey(context, rule),
            _ => null
        };

        if (string.IsNullOrWhiteSpace(key))
            return ValueTask.FromResult<string?>(null);

        if (string.IsNullOrWhiteSpace(rule.KeyPrefix))
            return ValueTask.FromResult<string?>(key);

        return ValueTask.FromResult<string?>($"{rule.KeyPrefix}:{key}");
    }

    private static string? ResolveCustomKey(RateLimitRequestContext context, RateLimitRequestRule rule)
    {
        if (string.IsNullOrWhiteSpace(rule.MetadataKey))
            return null;

        return context.Metadata.TryGetValue(rule.MetadataKey, out var value) ? value : null;
    }
}
