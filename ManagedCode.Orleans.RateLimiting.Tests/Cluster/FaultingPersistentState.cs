using ManagedCode.Orleans.RateLimiting.Server.Grains;
using Orleans.Runtime;

namespace ManagedCode.Orleans.RateLimiting.Tests.Cluster;

internal sealed class FaultingPersistentState<TOptions> : IPersistentState<RateLimiterGrainState<TOptions>> where TOptions : class
{
    internal const string FailureMessage = "Injected storage failure";
    public RateLimiterGrainState<TOptions> State { get; set; } = new();
    public string Etag => string.Empty;
    public bool RecordExists => true;
    internal bool FailWrites { get; set; }
    internal bool FailClears { get; set; }
    internal bool BlockWrites { get; set; }
    internal int Writes { get; private set; }
    internal CancellationToken WriteToken { get; private set; }
    internal TaskCompletionSource WriteStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task ReadStateAsync() => Task.CompletedTask;
    public Task ClearStateAsync() => FailClears ? Task.FromException(new IOException(FailureMessage)) : Task.CompletedTask;
    public Task WriteStateAsync() => WriteStateAsync(CancellationToken.None);

    public async Task WriteStateAsync(CancellationToken cancellationToken)
    {
        Writes++;
        WriteToken = cancellationToken;
        WriteStarted.TrySetResult();
        if (FailWrites)
            throw new IOException(FailureMessage);
        if (BlockWrites)
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }
}
