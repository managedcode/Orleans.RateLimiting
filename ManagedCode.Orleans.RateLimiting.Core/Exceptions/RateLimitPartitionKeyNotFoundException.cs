using System;
using ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;

namespace ManagedCode.Orleans.RateLimiting.Core.Exceptions;

public sealed class RateLimitPartitionKeyNotFoundException : Exception
{
    public RateLimitPartitionKeyNotFoundException()
    {
    }

    public RateLimitPartitionKeyNotFoundException(RateLimitPartitionKind kind)
        : base($"Rate limit partition key for '{kind}' was not found.")
    {
        Kind = kind;
    }

    public RateLimitPartitionKind? Kind { get; }
}
