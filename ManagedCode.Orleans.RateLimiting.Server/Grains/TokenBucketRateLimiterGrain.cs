using System;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using ManagedCode.Orleans.RateLimiting.Server.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace ManagedCode.Orleans.RateLimiting.Server.Grains;

[Reentrant]
[GrainType(RateLimiterGrainTypeNames.TokenBucketRateLimiter)]
public class TokenBucketRateLimiterGrain : RateLimiterGrain<TokenBucketRateLimiter, TokenBucketRateLimiterOptions>, ITokenBucketRateLimiterGrain
{
    public TokenBucketRateLimiterGrain(
        ILogger<TokenBucketRateLimiterGrain> logger,
        IOptions<TokenBucketRateLimiterOptions> options,
        IOptions<RateLimiterPersistenceOptions> persistenceOptions,
        [PersistentState(RateLimiterStorageNames.StateName, RateLimiterStorageNames.StorageProviderName)] IPersistentState<RateLimiterGrainState<TokenBucketRateLimiterOptions>> state)
        : base(logger, options.Value, state, persistenceOptions)
    {
    }

    protected override int PermitLimit => Options.TokenLimit;

    public async ValueTask<bool> TryReplenishAsync()
    {
        return await TryReplenishAndPersistAsync();
    }

    public async Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(TokenBucketRateLimiterOptions options)
    {
        if (CheckOptions(options))
            await ConfigureAsync(options);

        return await AcquireAsync();
    }

    public async Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(int permitCount, TokenBucketRateLimiterOptions options)
    {
        if (CheckOptions(options))
            await ConfigureAsync(options);

        return await AcquireAsync(permitCount);
    }

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

        var replenishedPeriods = (nowUtc - savedAtUtc).Ticks / Options.ReplenishmentPeriod.Ticks;
        var replenishedPermits = replenishedPeriods * Options.TokensPerPeriod;
        return (int)Math.Min(PermitLimit, savedAvailablePermits + replenishedPermits);
    }

    private bool CheckOptions(TokenBucketRateLimiterOptions options)
    {
        return Options.TokenLimit != options.TokenLimit || Options.QueueLimit != options.QueueLimit || Options.QueueProcessingOrder != options.QueueProcessingOrder ||
               Options.ReplenishmentPeriod != options.ReplenishmentPeriod || Options.AutoReplenishment != options.AutoReplenishment ||
               Options.TokensPerPeriod != options.TokensPerPeriod;
    }
}
