using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;

[GenerateSerializer]
public sealed class RateLimitRequestRule
{
    public RateLimitRequestRule()
    {
    }

    public RateLimitRequestRule(RateLimitPartitionKind kind, string configurationName)
    {
        Kind = kind;
        ConfigurationName = configurationName;
    }

    [Id(0)]
    public RateLimitPartitionKind Kind { get; init; }

    [Id(1)]
    public string ConfigurationName { get; init; } = string.Empty;

    [Id(2)]
    public string? Key { get; init; }

    [Id(3)]
    public string? MetadataKey { get; init; }

    [Id(4)]
    public string? KeyPrefix { get; init; }

    [Id(5)]
    public bool Required { get; init; }
}
