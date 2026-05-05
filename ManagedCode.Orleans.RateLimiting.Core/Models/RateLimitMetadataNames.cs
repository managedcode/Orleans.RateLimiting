namespace ManagedCode.Orleans.RateLimiting.Core.Models;

public static class RateLimitMetadataNames
{
    public const string ReasonPhrase = "REASON_PHRASE";
    public const string RetryAfter = "RETRY_AFTER";
    public const string LeaseNotAcquiredReason = "Lease not acquired";
    public const string RateLimitExceededReason = "Rate limit exceeded";
    public const string TimeLimitExceededReason = "Time limit exceeded";
}
