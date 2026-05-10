using System;
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

[GrainType(RateLimiterGrainTypeNames.SlidingWindowRateLimiter)]
public class SlidingWindowRateLimiterGrain : RateLimiterGrain<SlidingWindowRateLimiter, SlidingWindowRateLimiterOptions>, ISlidingWindowRateLimiterGrain
{
    public SlidingWindowRateLimiterGrain(
        ILogger<SlidingWindowRateLimiterGrain> logger,
        IOptions<SlidingWindowRateLimiterOptions> options,
        IOptions<RateLimiterPersistenceOptions> persistenceOptions,
        [PersistentState(RateLimiterStorageNames.StateName, RateLimiterStorageNames.StorageProviderName)] IPersistentState<RateLimiterGrainState<SlidingWindowRateLimiterOptions>> state)
        : base(logger, options.Value, state, persistenceOptions)
    {
    }

    protected override int PermitLimit => Options.PermitLimit;

    public async ValueTask<bool> TryReplenishAsync()
    {
        return await TryReplenishAndPersistAsync();
    }

    public async Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(SlidingWindowRateLimiterOptions options)
    {
        return await AcquireAndCheckConfigurationAsync(options, CheckOptions);
    }

    public async Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(int permitCount, SlidingWindowRateLimiterOptions options)
    {
        return await AcquireAndCheckConfigurationAsync(permitCount, options, CheckOptions);
    }

    protected override SlidingWindowRateLimiter CreateDefaultRateLimiter()
    {
        return new SlidingWindowRateLimiter(Options);
    }

    protected override bool TryReplenish()
    {
        return RateLimiter.TryReplenish();
    }

    protected override int GetRestoredAvailablePermits(DateTimeOffset savedAtUtc, int savedAvailablePermits, DateTimeOffset nowUtc)
    {
        if (Options.AutoReplenishment && nowUtc - savedAtUtc >= Options.Window)
            return PermitLimit;

        return base.GetRestoredAvailablePermits(savedAtUtc, savedAvailablePermits, nowUtc);
    }

    private bool CheckOptions(SlidingWindowRateLimiterOptions options)
    {
        return Options.PermitLimit != options.PermitLimit || Options.QueueLimit != options.QueueLimit || Options.QueueProcessingOrder != options.QueueProcessingOrder ||
               Options.Window != options.Window || Options.AutoReplenishment != options.AutoReplenishment || Options.SegmentsPerWindow != options.SegmentsPerWindow;
    }
}
