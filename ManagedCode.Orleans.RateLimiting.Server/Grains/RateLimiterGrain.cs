using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using Microsoft.Extensions.Logging;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Server.Grains;

public abstract class RateLimiterGrain<TLimiter, TOptions> : Grain, IDisposable where TLimiter : RateLimiter
{
    private const int NoActiveAcquires = 0;
    private const int SingleSemaphoreSlot = 1;

    private readonly ConcurrentDictionary<Guid, RateLimitLease> _rateLimitLeases = new();
    private readonly SemaphoreSlim _configurationLock = new(SingleSemaphoreSlot, SingleSemaphoreSlot);
    private readonly ILogger _logger;
    private readonly object _limiterLifetimeSync = new();
    private int _activeAcquireCount;
    private bool _configurationLockDisposed;
    private TaskCompletionSource? _noActiveAcquires;
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
        await EnterAcquireAsync();
        try
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
        finally
        {
            ExitAcquire();
        }
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

    public async ValueTask ConfigureAsync(TOptions options)
    {
        await _configurationLock.WaitAsync();
        try
        {
            await WaitForActiveAcquiresAsync();
            DisposeRateLimiter();
            _options = options;
            RateLimiter = CreateDefaultRateLimiter();
            _logger.LogInformation(RateLimiterLogMessages.ConfiguredLimiter, typeof(TLimiter).Name, this.GetPrimaryKeyString());
        }
        finally
        {
            _configurationLock.Release();
        }
    }

    public ValueTask<TOptions> GetConfiguration()
    {
        return ValueTask.FromResult(_options);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await _configurationLock.WaitAsync(cancellationToken);
        try
        {
            await WaitForActiveAcquiresAsync(cancellationToken);
            DisposeRateLimiter();
        }
        finally
        {
            _configurationLock.Release();
            DisposeConfigurationLock();
        }

        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    public void Dispose()
    {
        DisposeRateLimiter();
        DisposeConfigurationLock();
        GC.SuppressFinalize(this);
    }

    private async Task EnterAcquireAsync()
    {
        await _configurationLock.WaitAsync();
        try
        {
            lock (_limiterLifetimeSync)
            {
                _activeAcquireCount++;
            }
        }
        finally
        {
            _configurationLock.Release();
        }
    }

    private void ExitAcquire()
    {
        TaskCompletionSource? completed = null;

        lock (_limiterLifetimeSync)
        {
            _activeAcquireCount--;
            if (_activeAcquireCount == NoActiveAcquires)
            {
                completed = _noActiveAcquires;
                _noActiveAcquires = null;
            }
        }

        completed?.TrySetResult();
    }

    private Task WaitForActiveAcquiresAsync(CancellationToken cancellationToken = default)
    {
        Task waitTask;

        lock (_limiterLifetimeSync)
        {
            if (_activeAcquireCount == NoActiveAcquires)
                return Task.CompletedTask;

            _noActiveAcquires ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            waitTask = _noActiveAcquires.Task;
        }

        return waitTask.WaitAsync(cancellationToken);
    }

    private void DisposeRateLimiter()
    {
        foreach (var lease in _rateLimitLeases.Values)
            lease.Dispose();

        _rateLimitLeases.Clear();
        RateLimiter.Dispose();
    }

    private void DisposeConfigurationLock()
    {
        if (_configurationLockDisposed)
            return;

        _configurationLockDisposed = true;
        _configurationLock.Dispose();
    }
}
