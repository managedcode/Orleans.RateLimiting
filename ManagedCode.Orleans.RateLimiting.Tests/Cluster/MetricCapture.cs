using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace ManagedCode.Orleans.RateLimiting.Tests.Cluster;

internal sealed class MetricCapture : IDisposable
{
    internal const string MeterName = "ManagedCode.Orleans.RateLimiting";
    internal const string AcquisitionCounter = "rate_limiting.acquisitions";
    internal const string AcquisitionDuration = "rate_limiting.acquire.duration";
    internal const string ActivePermits = "rate_limiting.concurrency.active_permits";
    internal const string WriteCounter = "rate_limiting.state.writes";
    internal const string WriteDuration = "rate_limiting.state.write.duration";
    internal const string AlgorithmTag = "algorithm";
    internal const string OutcomeTag = "outcome";
    private readonly MeterListener _listener = new();
    internal ConcurrentQueue<Measurement> Measurements { get; } = new();

    internal MetricCapture(string? instrumentName = null)
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == MeterName && (instrumentName is null || instrument.Name == instrumentName))
                listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => Capture(instrument, value, tags));
        _listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => Capture(instrument, value, tags));
        _listener.Start();
    }

    private void Capture(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        => Measurements.Enqueue(new Measurement(instrument.Name, value, tags.ToArray().ToDictionary(tag => tag.Key, tag => tag.Value)));

    public void Dispose() => _listener.Dispose();
    internal sealed record Measurement(string Name, double Value, Dictionary<string, object?> Tags);
}
