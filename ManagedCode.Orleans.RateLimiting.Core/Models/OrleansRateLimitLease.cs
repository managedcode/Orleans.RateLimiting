using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Exceptions;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using Orleans;
using Orleans.Runtime;

namespace ManagedCode.Orleans.RateLimiting.Core.Models;

[DebuggerDisplay("IsAcquired = {IsAcquired}")]
public class OrleansRateLimitLease : IAsyncDisposable
{
    private readonly GrainId _grainId;
    private readonly Guid _guid;
    private readonly Dictionary<string, string?> _metadata;
    private bool _disposed;

    public OrleansRateLimitLease(RateLimitLeaseMetadata metadata, IGrainFactory grainFactory)
    {
        _guid = metadata.LeaseId;
        _grainId = metadata.GrainId;
        IsAcquired = metadata.IsAcquired;
        _metadata = metadata.Metadata.ToDictionary(k => k.Key, v => v.Value?.ToString());
        GrainFactory = grainFactory;
    }

    public IGrainFactory GrainFactory { get; init; }

    public string Reason => TryGetMetadata("REASON_PHRASE", out var reason) ? reason ?? string.Empty : "Rate limit exceeded";

    public TimeSpan RetryAfter =>
        TryGetMetadata("RETRY_AFTER", out var reason) && TimeSpan.TryParse(reason, CultureInfo.InvariantCulture, out var retryAfter) ? retryAfter : TimeSpan.Zero;

    public bool IsAcquired { get; init; }

    public IEnumerable<string> MetadataNames => _metadata.Select(s => s.Key);

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_guid == Guid.Empty)
        {
            GC.SuppressFinalize(this);
            return;
        }

        try
        {
            await GrainFactory.GetGrain(_grainId).AsReference<IRateLimiterGrain>().ReleaseLease(_guid);
        }
        catch (TimeoutException)
        {
            //ignore
        }
        catch (AggregateException ex) when (ex.InnerException is TimeoutException)
        {
            // ignore
        }

        GC.SuppressFinalize(this);
    }

    public void ThrowIfNotAcquired([CallerMemberName] string? caller = null, [CallerLineNumber] int? lineNumber = null, [CallerFilePath] string? filePath = null)
    {
        if (!IsAcquired)
            throw new RateLimitExceededException(Reason, RetryAfter);
    }

    public RateLimitExceededException ToException()
    {
        return new RateLimitExceededException(Reason, RetryAfter);
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
