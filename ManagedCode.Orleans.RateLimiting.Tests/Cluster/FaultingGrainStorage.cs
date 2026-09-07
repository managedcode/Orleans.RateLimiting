using Orleans.Runtime;
using Orleans.Storage;

namespace ManagedCode.Orleans.RateLimiting.Tests.Cluster;

internal sealed class StorageFaultControl
{
    internal bool FailWrites;
    internal int FailedWrites;
    internal int SuccessfulWrites;
}

internal sealed class FaultingGrainStorage(IGrainStorage inner, StorageFaultControl control) : IGrainStorage
{
    public Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        => inner.ReadStateAsync(stateName, grainId, grainState);

    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        => inner.ClearStateAsync(stateName, grainId, grainState);

    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        if (Volatile.Read(ref control.FailWrites))
        {
            Interlocked.Increment(ref control.FailedWrites);
            throw new IOException(FaultingPersistentState<object>.FailureMessage);
        }
        await inner.WriteStateAsync(stateName, grainId, grainState);
        Interlocked.Increment(ref control.SuccessfulWrites);
    }
}
