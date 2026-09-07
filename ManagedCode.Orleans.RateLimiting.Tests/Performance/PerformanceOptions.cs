using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using Orleans.Runtime;
using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;

namespace ManagedCode.Orleans.RateLimiting.Tests.Performance;

internal static class PerformanceOptions
{
    internal const int Operations = 32768;
    internal const int Warmup = 4096;
    internal const int SelectedAlgorithmWarmup = 65536;
    internal const string CallerTokenVariable = "RATE_LIMIT_PERFORMANCE_CALLER_TOKEN";
    internal const string AlgorithmVariable = "RATE_LIMIT_PERFORMANCE_ALGORITHM";
    internal const string WarmWorkloadVariable = "RATE_LIMIT_PERFORMANCE_WARM_WORKLOAD";
    internal const int Repetitions = 3;
    internal const int Parallelism = 16;
    internal const int Single = 1;
    internal const int Segments = 4;
    internal const int Quota = 1000000;
    internal const string Concurrency = "concurrency";
    internal const string Fixed = "fixed";
    internal const string Sliding = "sliding";
    internal const string Token = "token";
    internal const string Abandoned = "abandoned-time-leases";
    internal const string Sequential = "sequential";
    internal const string Hot = "hot-key-16";
    internal const string Partitioned = "partitioned-16";
    internal const string OutputVariable = "RATE_LIMIT_PERFORMANCE_OUTPUT";
    internal const string DefaultOutput = "artifacts/performance/result.json";
    private const int WindowHours = 1;
    private static readonly TimeSpan Window = TimeSpan.FromHours(WindowHours);

    internal static (ICancellableLimiterHolder Holder, GrainId GrainId) Create(IGrainFactory client, string algorithm)
    {
        var key = Guid.NewGuid().ToString();
        ICancellableLimiterHolder holder = algorithm switch
        {
            Concurrency => client.GetConcurrencyLimiter(key, new ConcurrencyLimiterOptions
            { PermitLimit = Quota, QueueLimit = Quota }),
            Fixed => client.GetFixedWindowRateLimiter(key, new FixedWindowRateLimiterOptions
            { PermitLimit = Quota, QueueLimit = Quota, Window = Window, AutoReplenishment = false }),
            Sliding => client.GetSlidingWindowRateLimiter(key, new SlidingWindowRateLimiterOptions
            { PermitLimit = Quota, QueueLimit = Quota, Window = Window, SegmentsPerWindow = Segments, AutoReplenishment = false }),
            Token => client.GetTokenBucketRateLimiter(key, new TokenBucketRateLimiterOptions
            { TokenLimit = Quota, QueueLimit = Quota, TokensPerPeriod = Quota, ReplenishmentPeriod = Window, AutoReplenishment = false }),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
        };
        IRateLimiterGrain grain = algorithm switch
        {
            Concurrency => client.GetGrain<IConcurrencyLimiterGrain>(key),
            Fixed => client.GetGrain<IFixedWindowRateLimiterGrain>(key),
            Sliding => client.GetGrain<ISlidingWindowRateLimiterGrain>(key),
            Token => client.GetGrain<ITokenBucketRateLimiterGrain>(key),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
        };
        return (holder, grain.GetGrainId());
    }
}
