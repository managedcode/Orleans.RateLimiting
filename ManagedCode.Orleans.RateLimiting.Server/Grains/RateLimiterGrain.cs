using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using ManagedCode.Orleans.RateLimiting.Server.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Runtime;

namespace ManagedCode.Orleans.RateLimiting.Server.Grains;

public abstract partial class RateLimiterGrain<TLimiter, TOptions> : Grain, IDisposable
    where TLimiter : RateLimiter
    where TOptions : class
{
    private const int NoActiveAcquires = 0;
    private const int NoAvailablePermits = 0;
    private const int NoQueuedPermits = 0;
    private const int SingleSemaphoreSlot = 1;

    private readonly SemaphoreSlim _configurationLock = new(SingleSemaphoreSlot, SingleSemaphoreSlot);
    private readonly TOptions _defaultOptions;
    private readonly ILogger _logger;
    private readonly object _limiterLifetimeSync = new();
    private readonly ConcurrentDictionary<Guid, RateLimitLease> _rateLimitLeases = new();
    private readonly TimeSpan _stateFlushPeriod;
    private readonly IPersistentState<RateLimiterGrainState<TOptions>> _state;
    private readonly SemaphoreSlim _stateLock = new(SingleSemaphoreSlot, SingleSemaphoreSlot);
    private int _activeAcquireCount;
    private bool _configurationLockDisposed;
    private TaskCompletionSource? _noActiveAcquires;
    private bool _stateDeleted;
    private bool _stateDirty;
    private IGrainTimer? _stateFlushTimer;
    private bool _stateLockDisposed;
    private TOptions _options;

    protected RateLimiterGrain(
        ILogger logger,
        TOptions options,
        IPersistentState<RateLimiterGrainState<TOptions>> state,
        IOptions<RateLimiterPersistenceOptions> persistenceOptions)
    {
        _logger = logger;
        _defaultOptions = options;
        _options = options;
        _state = state;
        _stateFlushPeriod = persistenceOptions.Value.StateFlushPeriod;
        RateLimiter = CreateDefaultRateLimiter();
    }

    protected TOptions Options => _options;

    protected TLimiter RateLimiter { get; private set; }

    protected virtual bool TracksActiveLeaseState => false;

    protected abstract int PermitLimit { get; }

    protected abstract TLimiter CreateDefaultRateLimiter();

    protected virtual bool TryReplenish()
    {
        return false;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        ApplyStoredConfiguration();
        ReplaceRateLimiter();
        await RestoreRateLimiterAsync();
        RegisterStateFlushTimer();
        await base.OnActivateAsync(cancellationToken);
    }

    public async Task<RateLimitLeaseMetadata> AcquireAsync(int permitCount = 1)
    {
        await EnterAcquireAsync();
        try
        {
            return await AcquireAndPersistAsync(permitCount);
        }
        finally
        {
            ExitAcquire();
        }
    }

    public async ValueTask ReleaseLease(Guid leaseId)
    {
        _rateLimitLeases.TryRemove(leaseId, out var lease);
        lease?.Dispose();

        if (!TracksActiveLeaseState)
            return;

        await MutateStateAsync(state =>
        {
            RemoveActiveLeaseState(state, leaseId);
            CaptureRuntimeSnapshot(state);
        });
    }

    public ValueTask<RateLimiterStatistics?> GetStatisticsAsync()
    {
        var runtimeStatistics = RateLimiter.GetStatistics();
        if (!_state.State.HasSnapshot)
            return ValueTask.FromResult(runtimeStatistics);

        return ValueTask.FromResult<RateLimiterStatistics?>(CreatePersistedStatistics(runtimeStatistics));
    }

    public async ValueTask ConfigureAsync(TOptions options)
    {
        await _configurationLock.WaitAsync();
        try
        {
            await WaitForActiveAcquiresAsync();
            await ConfigureLimiterAsync(options);
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

    public async ValueTask ResetAsync()
    {
        await _configurationLock.WaitAsync();
        try
        {
            await WaitForActiveAcquiresAsync();
            DisposeRateLimiter();
            RateLimiter = CreateDefaultRateLimiter();
            await MutateStateAsync(ResetQuotaState, flushImmediately: true);
        }
        finally
        {
            _configurationLock.Release();
        }
    }

    public async ValueTask DeleteStateAsync()
    {
        await _configurationLock.WaitAsync();
        try
        {
            await WaitForActiveAcquiresAsync();
            DisposeRateLimiter();
            _options = _defaultOptions;
            await ClearStoredStateAsync();
            RateLimiter = CreateDefaultRateLimiter();
        }
        finally
        {
            _configurationLock.Release();
        }
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await _configurationLock.WaitAsync(cancellationToken);
        try
        {
            await WaitForActiveAcquiresAsync(cancellationToken);

            if (!_stateDeleted || _stateDirty)
                await MutateStateAsync(CaptureRuntimeSnapshot, flushImmediately: true);
        }
        finally
        {
            DisposeRateLimiter();
            DisposeStateFlushTimer();
            _configurationLock.Release();
            DisposeConfigurationLock();
            DisposeStateLock();
        }

        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    public void Dispose()
    {
        DisposeRateLimiter();
        DisposeStateFlushTimer();
        DisposeConfigurationLock();
        DisposeStateLock();
        GC.SuppressFinalize(this);
    }

    protected async ValueTask<bool> TryReplenishAndPersistAsync()
    {
        var replenished = TryReplenish();
        if (replenished)
            await MutateStateAsync(CaptureRuntimeSnapshot);

        return replenished;
    }

    protected virtual int GetRestoredAvailablePermits(DateTimeOffset savedAtUtc, int savedAvailablePermits, DateTimeOffset nowUtc)
    {
        return ClampAvailablePermits(savedAvailablePermits);
    }

    protected async Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(TOptions options, Func<TOptions, bool> optionsChanged)
    {
        return await AcquireAndCheckConfigurationAsync(permitCount: 1, options, optionsChanged);
    }

    protected async Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(int permitCount, TOptions options, Func<TOptions, bool> optionsChanged)
    {
        await EnterAcquireAsync(options, optionsChanged);
        try
        {
            return await AcquireAndPersistAsync(permitCount);
        }
        finally
        {
            ExitAcquire();
        }
    }

    private async Task ConfigureLimiterAsync(TOptions options)
    {
        DisposeRateLimiter();
        _options = options;
        RateLimiter = CreateDefaultRateLimiter();
        await MutateStateAsync(state => ResetStateForConfiguration(state, options), flushImmediately: true);
        _logger.LogInformation(RateLimiterLogMessages.ConfiguredLimiter, typeof(TLimiter).Name, this.GetPrimaryKeyString());
    }

    private async Task<RateLimitLeaseMetadata> AcquireAndPersistAsync(int permitCount)
    {
        var leaseId = Guid.NewGuid();
        var lease = await RateLimiter.AcquireAsync(permitCount);
        var metadata = new RateLimitLeaseMetadata(leaseId, this.GetGrainId(), lease);

        if (!lease.IsAcquired)
            return await PersistRejectedLeaseAsync(lease, metadata);

        await PersistAcquiredLeaseAsync(leaseId, lease, permitCount);
        return metadata;
    }

    private async Task<RateLimitLeaseMetadata> PersistRejectedLeaseAsync(RateLimitLease lease, RateLimitLeaseMetadata metadata)
    {
        lease.Dispose();
        await MutateStateAsync(state =>
        {
            state.TotalFailedLeases++;
            CaptureRuntimeSnapshot(state);
        });

        return metadata;
    }

    private async Task PersistAcquiredLeaseAsync(Guid leaseId, RateLimitLease lease, int permitCount)
    {
        await MutateStateAsync(state =>
        {
            state.TotalSuccessfulLeases++;
            AddActiveLeaseState(state, leaseId, permitCount);
            CaptureRuntimeSnapshot(state);
        });

        if (!_rateLimitLeases.TryAdd(leaseId, lease))
            lease.Dispose();
    }

    private void ApplyStoredConfiguration()
    {
        if (_state.State.HasConfiguration && _state.State.Options is not null)
            _options = _state.State.Options;
    }

    private RateLimiterStatistics CreatePersistedStatistics(RateLimiterStatistics? runtimeStatistics)
    {
        return new RateLimiterStatistics
        {
            CurrentAvailablePermits = runtimeStatistics?.CurrentAvailablePermits ?? _state.State.CurrentAvailablePermits,
            CurrentQueuedCount = runtimeStatistics?.CurrentQueuedCount ?? NoQueuedPermits,
            TotalFailedLeases = _state.State.TotalFailedLeases,
            TotalSuccessfulLeases = _state.State.TotalSuccessfulLeases
        };
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

    private async Task EnterAcquireAsync(TOptions options, Func<TOptions, bool> optionsChanged)
    {
        await _configurationLock.WaitAsync();
        try
        {
            if (optionsChanged(options))
            {
                await WaitForActiveAcquiresAsync();
                await ConfigureLimiterAsync(options);
            }

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

    private int ClampAvailablePermits(int availablePermits)
    {
        return Math.Clamp(availablePermits, NoAvailablePermits, PermitLimit);
    }

    private void DisposeConfigurationLock()
    {
        if (_configurationLockDisposed)
            return;

        _configurationLockDisposed = true;
        _configurationLock.Dispose();
    }

    private void DisposeRateLimiter()
    {
        foreach (var lease in _rateLimitLeases.Values)
            lease.Dispose();

        _rateLimitLeases.Clear();
        RateLimiter.Dispose();
    }

    private void DisposeStateLock()
    {
        if (_stateLockDisposed)
            return;

        _stateLockDisposed = true;
        _stateLock.Dispose();
    }

    private void ReplaceRateLimiter()
    {
        DisposeRateLimiter();
        RateLimiter = CreateDefaultRateLimiter();
    }

}
