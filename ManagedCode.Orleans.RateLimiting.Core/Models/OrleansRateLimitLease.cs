using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using Orleans;
using Orleans.Runtime;

namespace ManagedCode.Orleans.RateLimiting.Core.Models;

public class OrleansRateLimitLease : IDisposable, IAsyncDisposable
{
    private readonly GrainId _grainId;
    private readonly Guid _guid;
    private readonly KeyValuePair<string, object?>[] _metadata;

    public OrleansRateLimitLease(RateLimitLeaseMetadata metadata, IGrainFactory grainFactory)
    {
        _guid = metadata.LeaseId;
        _grainId = metadata.GrainId;
        IsAcquired = metadata.IsAcquired;
        _metadata = metadata.Metadata;
        GrainFactory = grainFactory;
    }

    public IGrainFactory GrainFactory { get; init; }

    public bool IsAcquired { get; init; }
    public IEnumerable<string> MetadataNames => _metadata.Select(s => s.Key);

    public ValueTask DisposeAsync()
    {
        return GrainFactory.GetGrain(_grainId).AsReference<IRateLimiterGrain>().ReleaseLease(_guid);
    }

    public void Dispose()
    {
        Task.WaitAll(GrainFactory.GetGrain(_grainId).AsReference<IRateLimiterGrain>().ReleaseLease(_guid).AsTask());
    }

    public virtual IEnumerable<KeyValuePair<string, object?>> GetAllMetadata()
    {
        foreach (var name in MetadataNames)
            if (TryGetMetadata(name, out var metadata))
                yield return new KeyValuePair<string, object?>(name, metadata);
    }

    public bool TryGetMetadata(string metadataName, out object? metadata)
    {
        foreach (var pair in _metadata)
        {
            if (pair.Key == metadataName)
            {
                metadata = pair.Value;
                return true;
            }
        }
 

        metadata = null;
        return false;
    }
}