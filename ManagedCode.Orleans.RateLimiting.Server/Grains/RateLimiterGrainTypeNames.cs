namespace ManagedCode.Orleans.RateLimiting.Server.Grains;

internal static class RateLimiterGrainTypeNames
{
    private const string Prefix = "ManagedCode.$";

    public const string ConcurrencyLimiter = Prefix + nameof(ConcurrencyLimiterGrain);
    public const string FixedWindowRateLimiter = Prefix + nameof(FixedWindowRateLimiterGrain);
    public const string SlidingWindowRateLimiter = Prefix + nameof(SlidingWindowRateLimiterGrain);
    public const string TokenBucketRateLimiter = Prefix + nameof(TokenBucketRateLimiterGrain);
}
