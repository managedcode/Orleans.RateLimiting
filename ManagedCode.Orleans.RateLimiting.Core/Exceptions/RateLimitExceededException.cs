using System;

namespace ManagedCode.Orleans.RateLimiting.Core.Exceptions;

public class RateLimitExceededException : Exception
{
    public RateLimitExceededException() : base("Rate limit exceeded")
    {
        Reason = "Rate limit exceeded";
        RetryAfter = TimeSpan.Zero;
    }

    public RateLimitExceededException(string reason) : base(reason)
    {
        Reason = reason;
        RetryAfter = TimeSpan.Zero;
    }

    public RateLimitExceededException(TimeSpan retry) : base("Time limit exceeded")
    {
        Reason = "Time limit exceeded";
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