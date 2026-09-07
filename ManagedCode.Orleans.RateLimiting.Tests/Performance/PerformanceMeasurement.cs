using ManagedCode.Orleans.RateLimiting.Core.Models;
using System.Diagnostics;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;

namespace ManagedCode.Orleans.RateLimiting.Tests.Performance;

internal sealed record PerformanceMeasurement(string Algorithm, string Scenario, bool Cancellable, int Repetition,
    int Operations, double OperationsPerSecond, double P50Milliseconds, double P95Milliseconds, double P99Milliseconds,
    double ProcessAllocatedBytesPerOperation, long ProcessRetainedBytesDelta, int Gen0Collections, int Gen1Collections, int Gen2Collections)
{
    public bool TokenCanBeCanceled { get; init; }

    private const double Median = 0.50;
    private const double Tail95 = 0.95;
    private const double Tail99 = 0.99;
    private const int Gen0 = 0;
    private const int Gen1 = 1;
    private const int Gen2 = 2;
    private const int LastIndexOffset = 1;

    internal static async Task<PerformanceMeasurement> RunAsync(string algorithm, string scenario, bool cancellable,
        int repetition, ICancellableLimiterHolder[] holders, int workers, CancellationToken cancellationToken = default)
    {
        var times = new double[PerformanceOptions.Operations];
        var retainedBefore = GC.GetTotalMemory(forceFullCollection: true);
        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        var collections = new[] { GC.CollectionCount(Gen0), GC.CollectionCount(Gen1), GC.CollectionCount(Gen2) };
        var started = Stopwatch.GetTimestamp();
        await Task.WhenAll(Enumerable.Range(default, workers).Select(Worker));
        var elapsed = Stopwatch.GetElapsedTime(started);
        var allocated = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
        var collected = new[] { GC.CollectionCount(Gen0) - collections[Gen0], GC.CollectionCount(Gen1) - collections[Gen1], GC.CollectionCount(Gen2) - collections[Gen2] };
        var retained = GC.GetTotalMemory(forceFullCollection: true) - retainedBefore;
        Array.Sort(times);
        return new(algorithm, scenario, cancellable, repetition, times.Length, times.Length / elapsed.TotalSeconds,
            Percentile(times, Median), Percentile(times, Tail95), Percentile(times, Tail99), (double)allocated / times.Length,
            retained, collected[Gen0], collected[Gen1], collected[Gen2])
        { TokenCanBeCanceled = cancellationToken.CanBeCanceled };

        async Task Worker(int worker)
        {
            var holder = holders[worker % holders.Length];
            for (var index = worker; index < times.Length; index += workers)
            {
                var operationStarted = Stopwatch.GetTimestamp();
                await AcquireReleaseAsync(holder, cancellable, scenario != PerformanceOptions.Abandoned, cancellationToken);
                times[index] = Stopwatch.GetElapsedTime(operationStarted).TotalMilliseconds;
            }
        }
    }

    internal static async Task AcquireReleaseAsync(ICancellableLimiterHolder holder, bool cancellable, bool dispose = true, CancellationToken cancellationToken = default)
    {
        var lease = cancellable
            ? await holder.AcquireAndConfigureAsync(PerformanceOptions.Single, cancellationToken)
            : await AcquireWithoutTokenAsync(holder);
        try { lease.IsAcquired.ShouldBeTrue(); }
        finally { if (dispose) await lease.DisposeAsync(); }
    }

    private static Task<OrleansRateLimitLease> AcquireWithoutTokenAsync(ICancellableLimiterHolder holder) => holder.AcquireAndConfigureAsync();

    private static double Percentile(double[] sorted, double percentile)
        => sorted[(int)Math.Ceiling(sorted.Length * percentile) - LastIndexOffset];
}
