using System.Collections.Generic;
using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Attributes;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Server.GrainCallFilter;

public class FixedWindowRateLimiterIncomingFilter : BaseRateLimitingIncomingFilter<FixedWindowRateLimiterAttribute, FixedWindowRateLimiterOptions>
{
    public FixedWindowRateLimiterIncomingFilter(IGrainFactory grainFactory, IEnumerable<RateLimiterConfig> rateLimiterConfigs) : base(grainFactory, rateLimiterConfigs)
    {
    }

    protected override ILimiterHolderWithConfiguration<FixedWindowRateLimiterOptions> GetLimiter(string key)
    {
        return GrainFactory.GetFixedWindowRateLimiter(key);
    }
}