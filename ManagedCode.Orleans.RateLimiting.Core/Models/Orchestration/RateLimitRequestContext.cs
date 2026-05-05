using System.Collections.Generic;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;

[GenerateSerializer]
public sealed class RateLimitRequestContext
{
    [Id(0)]
    public string OperationName { get; init; } = string.Empty;

    [Id(1)]
    public string? UserId { get; init; }

    [Id(2)]
    public string? GroupId { get; init; }

    [Id(3)]
    public string? TenantId { get; init; }

    [Id(4)]
    public string? Role { get; init; }

    [Id(5)]
    public string? IpAddress { get; init; }

    [Id(6)]
    public string? Resource { get; init; }

    [Id(7)]
    public Dictionary<string, string> Metadata { get; init; } = new();
}
