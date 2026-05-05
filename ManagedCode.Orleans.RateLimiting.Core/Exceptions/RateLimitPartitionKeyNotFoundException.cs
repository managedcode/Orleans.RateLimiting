using System;
using ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;

namespace ManagedCode.Orleans.RateLimiting.Core.Exceptions;

public sealed class RateLimitPartitionKeyNotFoundException : Exception
{
    public RateLimitPartitionKeyNotFoundException()
    {
    }

    public RateLimitPartitionKeyNotFoundException(RateLimitPartitionKind kind)
        : base(RateLimiterExceptionMessages.PartitionKeyNotFound(kind))
    {
        Kind = kind;
    }

    public RateLimitPartitionKind? Kind { get; }
}
