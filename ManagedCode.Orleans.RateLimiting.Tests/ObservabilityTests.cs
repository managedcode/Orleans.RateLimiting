using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;

namespace ManagedCode.Orleans.RateLimiting.Tests;

[ClassDataSource<TestClusterApplication>(Shared = SharedType.PerTestSession)]
public class ObservabilityTests(TestClusterApplication testApp)
{
    private const int PermitCount = 1;
    private const int QueueLimit = 1;
    private const int InvalidPermitCount = -1;
    private const int TagCount = 2;
    private const int TimeoutSeconds = 10;
    private const string Acquired = "acquired";
    private const string Rejected = "rejected";
    private const string Cancelled = "cancelled";
    private const string Failed = "failed";
    private const string Written = "written";
    private const string Algorithm = "concurrency";

    [Test]
    public async Task MetricsCoverResultsCancellationAndWritesWithoutPartitionIdentifiers()
    {
        using var capture = new MetricCapture();
        var key = Guid.NewGuid().ToString();
        var holder = testApp.Cluster.Client.GetConcurrencyLimiter(key, new ConcurrencyLimiterOptions
        { PermitLimit = PermitCount, QueueLimit = QueueLimit });
        await using var held = await holder.AcquireAndConfigureAsync();
        using var cancellation = new CancellationTokenSource();
        var pending = holder.AcquireAsync(PermitCount, cancellation.Token);
        await CancellationTests.WaitForQueueAsync(holder, PermitCount);
        await using var rejected = await holder.AcquireAsync();
        rejected.IsAcquired.ShouldBeFalse();
        await cancellation.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(() => pending.WaitAsync(TimeSpan.FromSeconds(TimeoutSeconds)));
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => holder.AcquireAsync(InvalidPermitCount));
        await held.DisposeAsync();
        var activeChanges = capture.Measurements.Where(item => item.Name == MetricCapture.ActivePermits).ToArray();
        activeChanges.ShouldContain(item => item.Value == PermitCount);
        activeChanges.ShouldContain(item => item.Value == -PermitCount);
        activeChanges.Sum(item => item.Value).ShouldBe(default(double));
        var measurements = capture.Measurements.Where(item => item.Name != MetricCapture.ActivePermits).ToArray();
        var outcomes = measurements.Where(item => item.Name == MetricCapture.AcquisitionCounter)
            .Select(item => item.Tags[MetricCapture.OutcomeTag]).ToArray();
        foreach (var outcome in new[] { Acquired, Rejected, Cancelled, Failed })
            outcomes.ShouldContain(outcome);
        measurements.ShouldContain(item => item.Name == MetricCapture.WriteCounter && Equals(item.Tags[MetricCapture.OutcomeTag], Written));
        foreach (var item in measurements)
        {
            item.Value.ShouldBeGreaterThanOrEqualTo(default(double));
            item.Tags.Count.ShouldBe(TagCount);
            item.Tags.Keys.ShouldContain(MetricCapture.AlgorithmTag);
            item.Tags.Keys.ShouldContain(MetricCapture.OutcomeTag);
            item.Tags.Values.ShouldNotContain(key);
        }
        measurements.ShouldContain(item => Equals(item.Tags[MetricCapture.AlgorithmTag], Algorithm));
    }
}
