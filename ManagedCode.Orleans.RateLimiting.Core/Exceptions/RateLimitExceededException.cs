using System;
using Orleans;
using ManagedCode.Orleans.RateLimiting.Core.Models;

namespace ManagedCode.Orleans.RateLimiting.Core.Exceptions;

[GenerateSerializer]
public class RateLimitExceededException : Exception
{
    private const int ReasonFieldId = 0;
    private const int RetryAfterFieldId = 1;

    public RateLimitExceededException() : base(RateLimitMetadataNames.RateLimitExceededReason)
    {
        Reason = RateLimitMetadataNames.RateLimitExceededReason;
        RetryAfter = TimeSpan.Zero;
    }

    public RateLimitExceededException(string reason) : base(reason)
    {
        Reason = reason;
        RetryAfter = TimeSpan.Zero;
    }

    public RateLimitExceededException(TimeSpan retry) : base(RateLimitMetadataNames.TimeLimitExceededReason)
    {
        Reason = RateLimitMetadataNames.TimeLimitExceededReason;
        RetryAfter = retry;
    }

    public RateLimitExceededException(string reason, TimeSpan retry) : base(reason)
    {
        Reason = reason;
        RetryAfter = retry;
    }

    [Id(ReasonFieldId)]
    public string Reason { get; set; }
    [Id(RetryAfterFieldId)]
    public TimeSpan RetryAfter { get; set; }
}
