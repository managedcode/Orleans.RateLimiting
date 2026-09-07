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

[GrainType(RateLimiterGrainTypeNames.TokenBucketRateLimiter)]
public class TokenBucketRateLimiterGrain : RateLimiterGrain<TokenBucketRateLimiter, TokenBucketRateLimiterOptions>, ITokenBucketRateLimiterGrain, ICancellableRateLimiterGrain<TokenBucketRateLimiterOptions>, IBoundedRateLimiterGrain<TokenBucketRateLimiterOptions>
{
    private const int NoPermits = 0;

    public TokenBucketRateLimiterGrain(
        ILogger<TokenBucketRateLimiterGrain> logger,
        IOptions<TokenBucketRateLimiterOptions> options,
        IOptions<RateLimiterPersistenceOptions> persistenceOptions,
        [PersistentState(RateLimiterStorageNames.StateName, RateLimiterStorageNames.StorageProviderName)] IPersistentState<RateLimiterGrainState<TokenBucketRateLimiterOptions>> state)
        : base(logger, options.Value, state, persistenceOptions)
    {
    }

    protected override bool RequiresLeaseRelease => false;

    protected override bool IsQueueEnabled => Options.QueueLimit != default;

    protected override int PermitLimit => Options.TokenLimit;

    public async ValueTask<bool> TryReplenishAsync()
    {
        return await TryReplenishAndPersistAsync();
    }

    public async Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(TokenBucketRateLimiterOptions options)
    {
        return await AcquireAndCheckConfigurationAsync(options, CheckOptions);
    }

    public async Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(int permitCount, TokenBucketRateLimiterOptions options)
    {
        return await AcquireAndCheckConfigurationAsync(permitCount, options, CheckOptions);
    }

    public Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(int permitCount, TokenBucketRateLimiterOptions options, CancellationToken cancellationToken)
    {
        return AcquireAndCheckConfigurationAsync(permitCount, options, CheckOptions, cancellationToken);
    }

    public Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationWithDeadlineAsync(int permitCount, TokenBucketRateLimiterOptions options, TimeSpan timeout)
        => AcquireAndCheckConfigurationWithDeadlineAsync(permitCount, options, CheckOptions, timeout, CancellationToken.None);

    public Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationCancellableWithDeadlineAsync(int permitCount, TokenBucketRateLimiterOptions options, TimeSpan timeout, CancellationToken cancellationToken)
        => AcquireAndCheckConfigurationWithDeadlineAsync(permitCount, options, CheckOptions, timeout, cancellationToken);

    protected override TokenBucketRateLimiter CreateDefaultRateLimiter()
    {
        return new TokenBucketRateLimiter(Options);
    }

    protected override bool TryReplenish()
    {
        return RateLimiter.TryReplenish();
    }

    protected override int GetRestoredAvailablePermits(DateTimeOffset savedAtUtc, int savedAvailablePermits, DateTimeOffset nowUtc)
    {
        if (!Options.AutoReplenishment || Options.ReplenishmentPeriod <= TimeSpan.Zero)
            return base.GetRestoredAvailablePermits(savedAtUtc, savedAvailablePermits, nowUtc);

        var available = Math.Clamp(savedAvailablePermits, NoPermits, PermitLimit);
        var elapsed = nowUtc - savedAtUtc;
        if (elapsed <= TimeSpan.Zero)
            return available;

        var replenishedPeriods = elapsed.Ticks / Options.ReplenishmentPeriod.Ticks;
        var missing = PermitLimit - available;
        // Bound before multiplying: long outages and tiny periods must not overflow.
        if (replenishedPeriods > missing / Options.TokensPerPeriod)
            return PermitLimit;

        return available + (int)(replenishedPeriods * Options.TokensPerPeriod);
    }

    private bool CheckOptions(TokenBucketRateLimiterOptions options)
    {
        return Options.TokenLimit != options.TokenLimit || Options.QueueLimit != options.QueueLimit || Options.QueueProcessingOrder != options.QueueProcessingOrder ||
               Options.ReplenishmentPeriod != options.ReplenishmentPeriod || Options.AutoReplenishment != options.AutoReplenishment ||
               Options.TokensPerPeriod != options.TokensPerPeriod;
    }
}
