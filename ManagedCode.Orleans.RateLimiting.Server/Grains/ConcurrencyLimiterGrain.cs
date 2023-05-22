using System.Threading.RateLimiting;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Concurrency;

namespace ManagedCode.Orleans.RateLimiting.Server.Grains;

[Reentrant]
[GrainType($"ManagedCode.${nameof(ConcurrencyLimiterGrain)}")]
public class ConcurrencyLimiterGrain : RateLimiterGrain<ConcurrencyLimiter, ConcurrencyLimiterOptions>, IConcurrencyLimiterGrain
{
    public ConcurrencyLimiterGrain(ILogger<ConcurrencyLimiterGrain> logger, IOptions<ConcurrencyLimiterOptions> options) : base(logger, options.Value)
    {
    }

    public async Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(ConcurrencyLimiterOptions options)
    {
        if (CheckOptions(options))
            await ConfigureAsync(options);

        return await AcquireAsync();
    }

    public async Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(int permitCount, ConcurrencyLimiterOptions options)
    {
        if (CheckOptions(options))
            await ConfigureAsync(options);

        return await AcquireAsync(permitCount);
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