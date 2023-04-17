using System.Threading.RateLimiting;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Concurrency;

namespace ManagedCode.Orleans.RateLimiting.Server.Grains;

[Reentrant]
public class FixedWindowRateLimiterGrain : RateLimiterGrain<FixedWindowRateLimiter, FixedWindowRateLimiterOptions>,
    IFixedWindowRateLimiterGrain
{
    public FixedWindowRateLimiterGrain(ILogger<FixedWindowRateLimiterGrain> logger,
        IOptions<FixedWindowRateLimiterOptions> options) : base(logger, options.Value)
    {
    }

    public ValueTask<bool> TryReplenishAsync()
    {
        return ValueTask.FromResult(RateLimiter.TryReplenish());
    }

    protected override FixedWindowRateLimiter CreateDefaultRateLimiter()
    {
        return new FixedWindowRateLimiter(Options);
    }
    
    private bool CheckOptions(FixedWindowRateLimiterOptions options)
    {
        return Options.PermitLimit != options.PermitLimit
               || Options.QueueLimit != options.QueueLimit
               || Options.QueueProcessingOrder != options.QueueProcessingOrder
               || Options.Window != options.Window
               || Options.AutoReplenishment != options.AutoReplenishment
               ;
    }

    public async Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(FixedWindowRateLimiterOptions options)
    {
        if(CheckOptions(options))
        {
            await ConfigureAsync(options);
        }

        return await AcquireAsync();
    }

    public async Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(int permitCount, FixedWindowRateLimiterOptions options)
    {
        if(CheckOptions(options))
        {
            await ConfigureAsync(options);
        }

        return await AcquireAsync(permitCount);
    }
}