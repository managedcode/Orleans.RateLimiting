using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;

namespace ManagedCode.Orleans.RateLimiting.Server.Grains;

public abstract class RateLimiterGrain<TLimiter, TOptions> : Grain where TLimiter : RateLimiter
{
    private readonly ConcurrentDictionary<Guid, RateLimitLease> _rateLimitLeases = new();
    private readonly ILogger _logger;
    private TOptions _options;

    protected RateLimiterGrain(ILogger logger, TOptions options)
    {
        _logger = logger;
        _options = options;
        RateLimiter = CreateDefaultRateLimiter();
    }

    protected TOptions Options => _options;

    protected TLimiter RateLimiter { get; set; }

    protected abstract TLimiter CreateDefaultRateLimiter();

    public async Task<RateLimitLeaseMetadata> AcquireAsync(int permitCount = 1)
    {
        var leaseId = Guid.NewGuid();
        var lease = await RateLimiter.AcquireAsync(permitCount);
        var metadata = new RateLimitLeaseMetadata(leaseId, this.GetGrainId(), lease);

        if (lease.IsAcquired)
            _rateLimitLeases.TryAdd(leaseId, lease);
        else
            lease.Dispose();

        return metadata;
    }

    public ValueTask ReleaseLease(Guid leaseId)
    {
        _rateLimitLeases.TryRemove(leaseId, out var lease);
        lease?.Dispose();
        return ValueTask.CompletedTask;
    }

    public ValueTask<RateLimiterStatistics?> GetStatisticsAsync()
    {
        return ValueTask.FromResult(RateLimiter.GetStatistics());
    }

    public ValueTask ConfigureAsync(TOptions options)
    {
        DisposeRateLimiter();
        _options = options;
        RateLimiter = CreateDefaultRateLimiter();
        _logger.LogInformation("Configured {LimiterType} with id:{GrainId}", typeof(TLimiter).Name, this.GetPrimaryKeyString());
        return ValueTask.CompletedTask;
    }

    public ValueTask<TOptions> GetConfiguration()
    {
        return ValueTask.FromResult(_options);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        DisposeRateLimiter();
        return base.OnDeactivateAsync(reason, cancellationToken);
    }

    private void DisposeRateLimiter()
    {
        foreach (var lease in _rateLimitLeases.Values)
            lease.Dispose();

        _rateLimitLeases.Clear();
        RateLimiter.Dispose();
    }
}
