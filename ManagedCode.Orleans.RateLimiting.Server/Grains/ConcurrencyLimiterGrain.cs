using System;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using ManagedCode.Orleans.RateLimiting.Server.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Runtime;

namespace ManagedCode.Orleans.RateLimiting.Server.Grains;

[GrainType(RateLimiterGrainTypeNames.ConcurrencyLimiter)]
public class ConcurrencyLimiterGrain : RateLimiterGrain<ConcurrencyLimiter, ConcurrencyLimiterOptions>, IConcurrencyLimiterGrain, ICancellableRateLimiterGrain<ConcurrencyLimiterOptions>, IBoundedRateLimiterGrain<ConcurrencyLimiterOptions>
{
    public ConcurrencyLimiterGrain(
        ILogger<ConcurrencyLimiterGrain> logger,
        IOptions<ConcurrencyLimiterOptions> options,
        IOptions<RateLimiterPersistenceOptions> persistenceOptions,
        [PersistentState(RateLimiterStorageNames.StateName, RateLimiterStorageNames.StorageProviderName)] IPersistentState<RateLimiterGrainState<ConcurrencyLimiterOptions>> state)
        : base(logger, options.Value, state, persistenceOptions)
    {
    }

    protected override bool TracksActiveLeaseState => true;

    protected override bool IsQueueEnabled => Options.QueueLimit != default;

    protected override int PermitLimit => Options.PermitLimit;

    public async Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(ConcurrencyLimiterOptions options)
    {
        return await AcquireAndCheckConfigurationAsync(options, CheckOptions);
    }

    public async Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(int permitCount, ConcurrencyLimiterOptions options)
    {
        return await AcquireAndCheckConfigurationAsync(permitCount, options, CheckOptions);
    }

    public Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(int permitCount, ConcurrencyLimiterOptions options, CancellationToken cancellationToken)
    {
        return AcquireAndCheckConfigurationAsync(permitCount, options, CheckOptions, cancellationToken);
    }

    public Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationWithDeadlineAsync(int permitCount, ConcurrencyLimiterOptions options, TimeSpan timeout)
        => AcquireAndCheckConfigurationWithDeadlineAsync(permitCount, options, CheckOptions, timeout, CancellationToken.None);

    public Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationCancellableWithDeadlineAsync(int permitCount, ConcurrencyLimiterOptions options, TimeSpan timeout, CancellationToken cancellationToken)
        => AcquireAndCheckConfigurationWithDeadlineAsync(permitCount, options, CheckOptions, timeout, cancellationToken);

    protected override ConcurrencyLimiter CreateDefaultRateLimiter()
    {
        return new ConcurrencyLimiter(Options);
    }

    private bool CheckOptions(ConcurrencyLimiterOptions options)
    {
        return Options.PermitLimit != options.PermitLimit || Options.QueueLimit != options.QueueLimit || Options.QueueProcessingOrder != options.QueueProcessingOrder;
    }
}
