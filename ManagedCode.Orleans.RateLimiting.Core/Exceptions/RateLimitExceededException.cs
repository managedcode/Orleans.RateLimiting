using System;

namespace ManagedCode.Orleans.RateLimiting.Core.Exceptions;

public class RateLimitExceededException : Exception
{
    public RateLimitExceededException()
    {
        Reason = "Rate limit exceeded";
        RetryAfter = TimeSpan.Zero;
    }

    public RateLimitExceededException(string reason)
    {
        Reason = reason;
        RetryAfter = TimeSpan.Zero;
    }

    public RateLimitExceededException(TimeSpan retry)
    {
        Reason = "Time limit exceeded";
        RetryAfter = retry;
    }

    public RateLimitExceededException(string reason, TimeSpan retry)
    {
        Reason = reason;
        RetryAfter = retry;
    }

    public string Reason { get; set; }
    public TimeSpan RetryAfter { get; set; }
}