using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Orleans.RateLimiting.Tests.Cluster;

internal static class SecurityGrainOptions
{
    public const string Strict = "grain-security-strict";
    public const string Loose = "grain-security-loose";
    public const string ConcurrencyKey = "grain-security-concurrency";
    public const string InlineConcurrencyKey = "grain-security-inline-concurrency";
    public const string InterfaceConcurrencyKey = "grain-security-interface-concurrency";
    public const string EmptyConfiguration = "";
    public const int StrictPermits = 1;
    public const int LoosePermits = 2;
    public const int QueueLimit = 0;
    public const int WindowSeconds = 600;
    public const int Segments = 2;

    public static void Configure(IServiceCollection services)
    {
        Add(services, Strict, StrictPermits);
        Add(services, Loose, LoosePermits);
    }

    private static void Add(IServiceCollection services, string name, int permits)
    {
        services.AddOrleansRateLimiterOptions(name, new ConcurrencyLimiterOptions
        {
            PermitLimit = permits,
            QueueLimit = QueueLimit
        });
        services.AddOrleansRateLimiterOptions(name, new FixedWindowRateLimiterOptions
        {
            PermitLimit = permits,
            QueueLimit = QueueLimit,
            Window = TimeSpan.FromSeconds(WindowSeconds)
        });
        services.AddOrleansRateLimiterOptions(name, new SlidingWindowRateLimiterOptions
        {
            PermitLimit = permits,
            QueueLimit = QueueLimit,
            Window = TimeSpan.FromSeconds(WindowSeconds),
            SegmentsPerWindow = Segments
        });
        services.AddOrleansRateLimiterOptions(name, new TokenBucketRateLimiterOptions
        {
            TokenLimit = permits,
            TokensPerPeriod = permits,
            QueueLimit = QueueLimit,
            ReplenishmentPeriod = TimeSpan.FromSeconds(WindowSeconds)
        });
    }
}
