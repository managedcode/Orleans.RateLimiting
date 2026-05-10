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
public class ConcurrencyLimiterGrain : RateLimiterGrain<ConcurrencyLimiter, ConcurrencyLimiterOptions>, IConcurrencyLimiterGrain
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

    protected override int PermitLimit => Options.PermitLimit;

    public async Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(ConcurrencyLimiterOptions options)
    {
        return await AcquireAndCheckConfigurationAsync(options, CheckOptions);
    }

    public async Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(int permitCount, ConcurrencyLimiterOptions options)
    {
        return await AcquireAndCheckConfigurationAsync(permitCount, options, CheckOptions);
    }

    protected override ConcurrencyLimiter CreateDefaultRateLimiter()
    {
        return new ConcurrencyLimiter(Options);
    }

    private bool CheckOptions(ConcurrencyLimiterOptions options)
    {
        return Options.PermitLimit != options.PermitLimit || Options.QueueLimit != options.QueueLimit || Options.QueueProcessingOrder != options.QueueProcessingOrder;
    }
}
