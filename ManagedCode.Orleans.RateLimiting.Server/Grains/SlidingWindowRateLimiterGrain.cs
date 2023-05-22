using System.Threading.RateLimiting;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Concurrency;

namespace ManagedCode.Orleans.RateLimiting.Server.Grains;

[Reentrant]
public class SlidingWindowRateLimiterGrain : RateLimiterGrain<SlidingWindowRateLimiter, SlidingWindowRateLimiterOptions>, ISlidingWindowRateLimiterGrain
{
    public SlidingWindowRateLimiterGrain(ILogger<SlidingWindowRateLimiterGrain> logger, IOptions<SlidingWindowRateLimiterOptions> options) : base(logger, options.Value)
    {
    }

    public ValueTask<bool> TryReplenishAsync()
    {
        return ValueTask.FromResult(RateLimiter.TryReplenish());
    }

    public async Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(SlidingWindowRateLimiterOptions options)
    {
        if (CheckOptions(options))
            await ConfigureAsync(options);

        return await AcquireAsync();
    }

    public async Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(int permitCount, SlidingWindowRateLimiterOptions options)
    {
        if (CheckOptions(options))
            await ConfigureAsync(options);

        return await AcquireAsync(permitCount);
    }

    protected override SlidingWindowRateLimiter CreateDefaultRateLimiter()
    {
        return new SlidingWindowRateLimiter(Options);
    }

    private bool CheckOptions(SlidingWindowRateLimiterOptions options)
    {
        return Options.PermitLimit != options.PermitLimit || Options.QueueLimit != options.QueueLimit || Options.QueueProcessingOrder != options.QueueProcessingOrder ||
               Options.Window != options.Window || Options.AutoReplenishment != options.AutoReplenishment || Options.SegmentsPerWindow != options.SegmentsPerWindow;
    }
}