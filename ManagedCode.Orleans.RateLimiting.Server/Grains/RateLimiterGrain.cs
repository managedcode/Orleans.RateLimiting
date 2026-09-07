using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Server.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Runtime;

namespace ManagedCode.Orleans.RateLimiting.Server.Grains;

public abstract partial class RateLimiterGrain<TLimiter, TOptions> : Grain, IDisposable, ICancellableRateLimiterGrain, IBoundedRateLimiterGrain
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
    private readonly ConcurrentDictionary<Guid, TrackedLease> _rateLimitLeases = new();
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

    protected virtual TimeProvider Clock => GrainContext?.ActivationServices.GetService<TimeProvider>() ?? TimeProvider.System;

    protected TLimiter RateLimiter { get; private set; }

    protected virtual bool TracksActiveLeaseState => false;

    protected virtual bool RequiresLeaseRelease => true;

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

    public Task<RateLimitLeaseMetadata> AcquireAsync(int permitCount = 1) => AcquireAsync(permitCount, CancellationToken.None);

    public async Task<RateLimitLeaseMetadata> AcquireAsync(int permitCount, CancellationToken cancellationToken)
    {
        await EnterAcquireAsync(default, cancellationToken);
        try
        {
            return await AcquireAndPersistAsync(permitCount, default, cancellationToken);
        }
        finally
        {
            ExitAcquire();
        }
    }

    public async ValueTask ReleaseLease(Guid leaseId)
    {
        if (_rateLimitLeases.TryRemove(leaseId, out var lease))
            DisposeTrackedLease(lease);

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
            await ClearStoredStateAsync();
            DisposeRateLimiter();
            _options = _defaultOptions;
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
                await MutateStateAsync(CaptureRuntimeSnapshot, flushImmediately: true, cancelBeforeMutation: true, cancellationToken: cancellationToken);
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
            DisposeTrackedLease(lease);

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
