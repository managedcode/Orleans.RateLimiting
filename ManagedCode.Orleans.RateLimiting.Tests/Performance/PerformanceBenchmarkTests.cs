using System.Runtime.InteropServices;
using System.Text.Json;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;

namespace ManagedCode.Orleans.RateLimiting.Tests.Performance;

[ClassDataSource<PerformanceCluster>(Shared = SharedType.PerTestSession)]
public class PerformanceBenchmarkTests(PerformanceCluster app)
{
    private const string TopologyDescription = "Two silos; preferred gateway 0; verified grain placement on silo 0";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    [Test]
    public async Task MeasureAcquisitionAndRelease()
    {
        var results = new List<PerformanceMeasurement>();
        foreach (var algorithm in new[] { PerformanceOptions.Concurrency, PerformanceOptions.Fixed, PerformanceOptions.Sliding, PerformanceOptions.Token })
            foreach (var scenario in new[] { PerformanceOptions.Sequential, PerformanceOptions.Hot, PerformanceOptions.Partitioned, PerformanceOptions.Abandoned })
                foreach (var cancellable in new[] { false, true })
                    await MeasureAsync(results, algorithm, scenario, cancellable);

        results.ShouldNotBeEmpty();
        var output = Path.GetFullPath(Environment.GetEnvironmentVariable(PerformanceOptions.OutputVariable) ?? PerformanceOptions.DefaultOutput);
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        var report = new
        {
            RuntimeInformation.FrameworkDescription,
            RuntimeInformation.OSDescription,
            RuntimeInformation.ProcessArchitecture,
            Environment.ProcessorCount,
            Timestamp = DateTimeOffset.UtcNow,
            Topology = TopologyDescription,
            WarmWorkload = ShouldWarmWorkload(),
            MetadataPayload = PerformancePayload.Create(),
            Results = results
        };
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(report, JsonOptions));
        Console.WriteLine(output);
    }

    private async Task MeasureAsync(List<PerformanceMeasurement> results, string algorithm, string scenario, bool cancellable)
    {
        var hasCallerToken = bool.TryParse(Environment.GetEnvironmentVariable(PerformanceOptions.CallerTokenVariable), out var enabled) && enabled;
        if (hasCallerToken && !cancellable) return;
        using var callerCancellation = hasCallerToken ? new CancellationTokenSource() : null;
        var callerToken = callerCancellation?.Token ?? CancellationToken.None;
        var selectedAlgorithm = Environment.GetEnvironmentVariable(PerformanceOptions.AlgorithmVariable);
        if (selectedAlgorithm is not null && selectedAlgorithm != algorithm)
            return;
        if (scenario == PerformanceOptions.Abandoned && algorithm == PerformanceOptions.Concurrency)
            return;
        var workers = scenario is PerformanceOptions.Sequential or PerformanceOptions.Abandoned ? PerformanceOptions.Single : PerformanceOptions.Parallelism;
        var partitionCount = scenario == PerformanceOptions.Partitioned ? workers : PerformanceOptions.Single;
        var partitions = Enumerable.Range(default, partitionCount).Select(_ => PerformanceOptions.Create(app.Cluster.Client, algorithm)).ToArray();
        foreach (var partition in partitions)
            await app.PlaceAsync(partition.Holder, partition.GrainId);
        var holders = partitions.Select(partition => partition.Holder).ToArray();
        var warmup = selectedAlgorithm is not null && partitionCount == PerformanceOptions.Single
            ? PerformanceOptions.SelectedAlgorithmWarmup : PerformanceOptions.Warmup;
        foreach (var holder in holders)
            for (var index = default(int); index < warmup; index++)
                await PerformanceMeasurement.AcquireReleaseAsync(holder, cancellable, cancellationToken: callerToken);
        if (ShouldWarmWorkload())
            await PerformanceMeasurement.RunAsync(algorithm, scenario, cancellable, default, holders, workers, callerToken);
        for (var repetition = default(int); repetition < PerformanceOptions.Repetitions; repetition++)
        {
            foreach (var partition in partitions) app.AssertPlacement(partition.GrainId);
            results.Add(await PerformanceMeasurement.RunAsync(algorithm, scenario, cancellable, repetition, holders, workers, callerToken));
            foreach (var partition in partitions) app.AssertPlacement(partition.GrainId);
        }
        foreach (var holder in holders)
            await holder.DeleteStateAsync();
    }

    private static bool ShouldWarmWorkload()
        => bool.TryParse(Environment.GetEnvironmentVariable(PerformanceOptions.WarmWorkloadVariable), out var enabled) && enabled;
}
