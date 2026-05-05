using System;
using ManagedCode.Orleans.RateLimiting.Core.Models;

namespace ManagedCode.Orleans.RateLimiting.Core.Exceptions;

public class RateLimitExceededException : Exception
{
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

    public string Reason { get; set; }
    public TimeSpan RetryAfter { get; set; }
}
