using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading.RateLimiting;

namespace ManagedCode.Orleans.RateLimiting.Server.Diagnostics;

internal static class RateLimiterMetrics
{
    internal const string MeterName = "ManagedCode.Orleans.RateLimiting";
    internal const string Acquired = "acquired";
    internal const string Rejected = "rejected";
    internal const string Cancelled = "cancelled";
    internal const string Failed = "failed";
    internal const string Written = "written";
    private const string AlgorithmTag = "algorithm";
    private const string OutcomeTag = "outcome";
    private const string ActivePermitsName = "rate_limiting.concurrency.active_permits";
    private const string Seconds = "s";
    private const string AcquisitionsName = "rate_limiting.acquisitions";
    private const string AcquisitionDurationName = "rate_limiting.acquire.duration";
    private const string StateWritesName = "rate_limiting.state.writes";
    private const string StateWriteDurationName = "rate_limiting.state.write.duration";
    private const string ConcurrencyAlgorithm = "concurrency";
    private const string FixedWindowAlgorithm = "fixed_window";
    private const string SlidingWindowAlgorithm = "sliding_window";
    private const string TokenBucketAlgorithm = "token_bucket";
    private const string CustomAlgorithm = "custom";
    private const int SingleEvent = 1;
    private static readonly Meter Meter = new(MeterName);
    private static readonly UpDownCounter<long> ActivePermits = Meter.CreateUpDownCounter<long>(ActivePermitsName);
    private static readonly Counter<long> Acquisitions = Meter.CreateCounter<long>(AcquisitionsName);
    private static readonly Histogram<double> AcquisitionDuration = Meter.CreateHistogram<double>(AcquisitionDurationName, Seconds);
    private static readonly Counter<long> StateWrites = Meter.CreateCounter<long>(StateWritesName);
    private static readonly Histogram<double> StateWriteDuration = Meter.CreateHistogram<double>(StateWriteDurationName, Seconds);

    internal static void ChangeActivePermits(int permits) => ActivePermits.Add(permits);

    internal static void RecordAcquisition<TLimiter>(string outcome, long started)
    {
        var tags = CreateTags<TLimiter>(outcome);
        Acquisitions.Add(SingleEvent, tags);
        AcquisitionDuration.Record(Stopwatch.GetElapsedTime(started).TotalSeconds, tags);
    }

    internal static void RecordStateWrite<TLimiter>(string outcome, long started)
    {
        var tags = CreateTags<TLimiter>(outcome);
        StateWrites.Add(SingleEvent, tags);
        StateWriteDuration.Record(Stopwatch.GetElapsedTime(started).TotalSeconds, tags);
    }

    private static TagList CreateTags<TLimiter>(string outcome)
    {
        // Fixed vocabulary even for consumer subclasses: no grain keys, identities or exception text.
        var algorithm = typeof(TLimiter) == typeof(ConcurrencyLimiter) ? ConcurrencyAlgorithm
            : typeof(TLimiter) == typeof(FixedWindowRateLimiter) ? FixedWindowAlgorithm
            : typeof(TLimiter) == typeof(SlidingWindowRateLimiter) ? SlidingWindowAlgorithm
            : typeof(TLimiter) == typeof(TokenBucketRateLimiter) ? TokenBucketAlgorithm : CustomAlgorithm;
        return new TagList { { AlgorithmTag, algorithm }, { OutcomeTag, outcome } };
    }
}
