using System;
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

    public override string ToString() => FormatKey(Kind, Key, ConfigurationName);

    internal static string FormatKey(RateLimitPartitionKind kind, string key, string? configurationName)
    {
        var kindName = Enum.GetName(kind) ?? kind.ToString();
        return configurationName is null
            ? RateLimitPartitionKeyFormatter.Join(kindName, key)
            : RateLimitPartitionKeyFormatter.Join(kindName, configurationName, key);
    }
}
