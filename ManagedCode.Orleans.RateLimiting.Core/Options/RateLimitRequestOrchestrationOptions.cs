using System.Collections.Generic;
using ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;

namespace ManagedCode.Orleans.RateLimiting.Core.Options;

public sealed class RateLimitRequestOrchestrationOptions
{
    public IList<RateLimitRequestRule> Rules { get; } = [];

    public RateLimitRequestOrchestrationOptions AddUser(string configurationName, bool required = false, string? keyPrefix = null)
    {
        return Add(RateLimitPartitionKind.User, configurationName, required, keyPrefix);
    }

    public RateLimitRequestOrchestrationOptions AddGroup(string configurationName, bool required = false, string? keyPrefix = null)
    {
        return Add(RateLimitPartitionKind.Group, configurationName, required, keyPrefix);
    }

    public RateLimitRequestOrchestrationOptions AddTenant(string configurationName, bool required = false, string? keyPrefix = null)
    {
        return Add(RateLimitPartitionKind.Tenant, configurationName, required, keyPrefix);
    }

    public RateLimitRequestOrchestrationOptions AddRole(string configurationName, bool required = false, string? keyPrefix = null)
    {
        return Add(RateLimitPartitionKind.Role, configurationName, required, keyPrefix);
    }

    public RateLimitRequestOrchestrationOptions AddIpAddress(string configurationName, bool required = false, string? keyPrefix = null)
    {
        return Add(RateLimitPartitionKind.IpAddress, configurationName, required, keyPrefix);
    }

    public RateLimitRequestOrchestrationOptions AddEndpoint(string configurationName, bool required = false, string? keyPrefix = null)
    {
        return Add(RateLimitPartitionKind.Endpoint, configurationName, required, keyPrefix);
    }

    public RateLimitRequestOrchestrationOptions AddGrain(string configurationName, bool required = false, string? keyPrefix = null)
    {
        return Add(RateLimitPartitionKind.Grain, configurationName, required, keyPrefix);
    }

    public RateLimitRequestOrchestrationOptions AddCustom(string configurationName, string metadataKey, bool required = false, string? keyPrefix = null)
    {
        Rules.Add(new RateLimitRequestRule(RateLimitPartitionKind.Custom, configurationName)
        {
            MetadataKey = metadataKey,
            Required = required,
            KeyPrefix = keyPrefix
        });

        return this;
    }

    public RateLimitRequestOrchestrationOptions Add(RateLimitPartitionKind kind, string configurationName, bool required = false, string? keyPrefix = null)
    {
        Rules.Add(new RateLimitRequestRule(kind, configurationName)
        {
            Required = required,
            KeyPrefix = keyPrefix
        });

        return this;
    }
}
