using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;

[GenerateSerializer]
public sealed class RateLimitRequestPartition
{
    public RateLimitRequestPartition()
    {
    }

    public RateLimitRequestPartition(RateLimitPartitionKind kind, string key, string? configurationName = null)
    {
        Kind = kind;
        Key = key;
        ConfigurationName = configurationName;
    }

    [Id(0)]
    public RateLimitPartitionKind Kind { get; init; }

    [Id(1)]
    public string Key { get; init; } = string.Empty;

    [Id(2)]
    public string? ConfigurationName { get; init; }

    public override string ToString()
    {
        return ConfigurationName is null ? $"{Kind}:{Key}" : $"{Kind}:{ConfigurationName}:{Key}";
    }
}
