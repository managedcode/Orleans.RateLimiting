using System;
using Orleans;
using ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;

namespace ManagedCode.Orleans.RateLimiting.Core.Exceptions;

[GenerateSerializer]
public sealed class RateLimitPartitionKeyNotFoundException : Exception
{
    private const int KindFieldId = 0;

    public RateLimitPartitionKeyNotFoundException()
    {
    }

    public RateLimitPartitionKeyNotFoundException(RateLimitPartitionKind kind)
        : base(RateLimiterExceptionMessages.PartitionKeyNotFound(kind))
    {
        Kind = kind;
    }

    [Id(KindFieldId)]
    public RateLimitPartitionKind? Kind { get; }
}
