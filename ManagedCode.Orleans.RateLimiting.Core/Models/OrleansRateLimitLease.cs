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
    private readonly Dictionary<string, string?> _metadata;

    public OrleansRateLimitLease(RateLimitLeaseMetadata metadata, IGrainFactory grainFactory)
    {
        _guid = metadata.LeaseId;
        _grainId = metadata.GrainId;
        IsAcquired = metadata.IsAcquired;
        _metadata = metadata.Metadata.ToDictionary(k => k.Key, v => v.Value?.ToString());
        GrainFactory = grainFactory;
    }

    public IGrainFactory GrainFactory { get; init; }

    public string Reason => TryGetMetadata("REASON_PHRASE", out var reason) ? reason ?? string.Empty : string.Empty;

    public TimeSpan RetryAfter => TryGetMetadata("RETRY_AFTER", out var reason)
        ? TimeSpan.Parse(reason ?? TimeSpan.Zero.ToString())
        : TimeSpan.Zero;

    public bool IsAcquired { get; init; }

    public IEnumerable<string> MetadataNames => _metadata.Select(s => s.Key);

    public async ValueTask DisposeAsync()
    {
        if (_guid == Guid.Empty)
            return;

        try
        {
            await GrainFactory.GetGrain(_grainId).AsReference<IRateLimiterGrain>().ReleaseLease(_guid);
        }
        catch (AggregateException ex) when (ex.InnerException is TimeoutException)
        {
            // ignore
        }
    }

    public void Dispose()
    {
        Task.WaitAll(DisposeAsync().AsTask());
    }

    public virtual IEnumerable<KeyValuePair<string, string?>> GetAllMetadata()
    {
        foreach (var pair in _metadata)
            yield return pair;
    }

    public bool TryGetMetadata(string metadataName, out string? metadata)
    {
        var result = _metadata.TryGetValue(metadataName, out var value);
        metadata = value;
        return result;
    }
}