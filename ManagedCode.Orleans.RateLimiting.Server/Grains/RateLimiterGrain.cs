using System;
using System.Collections.Generic;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;

namespace ManagedCode.Orleans.RateLimiting.Server.Grains;

[Reentrant]
public abstract class RateLimiterGrain<T> : Grain where T : RateLimiter
{
    protected readonly ILogger _logger;
    private readonly Dictionary<Guid, RateLimitLease> _rateLimitLeases = new();
    private RateLimiter _rateLimiter;

    protected RateLimiterGrain(ILogger logger)
    {
        _logger = logger;
    }

    protected T RateLimiter
    {
        get => (T)_rateLimiter;
        set => _rateLimiter = value;
    }

    public async Task<RateLimitLeaseMetadata> AcquireAsync(int permitCount = 1)
    {
        var guid = Guid.NewGuid();

        var lease = await Task.Run(async () => await _rateLimiter.AcquireAsync(permitCount));
        _rateLimitLeases.Add(guid, lease);

        var orleansLease = new RateLimitLeaseMetadata(guid, this.GetGrainId(), lease);
        return orleansLease;
    }

    public async ValueTask ReleaseLease(Guid guid)
    {
        await Task.Run(() =>
        {
            _rateLimitLeases.Remove(guid, out var lease);
            lease?.Dispose();
        });
    }

    public ValueTask<RateLimiterStatistics?> GetStatisticsAsync()
    {
        return ValueTask.FromResult(_rateLimiter.GetStatistics());
    }
}