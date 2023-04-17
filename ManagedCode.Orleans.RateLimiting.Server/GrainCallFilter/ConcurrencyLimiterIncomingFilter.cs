using System;
using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Attributes;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Server.GrainCallFilter;

public class ConcurrencyLimiterIncomingFilter : BaseRateLimitingIncomingFilter<ConcurrencyLimiterAttribute, ConcurrencyLimiterOptions>
{
    public ConcurrencyLimiterIncomingFilter(IGrainFactory grainFactory) : base(grainFactory)
    {
    }

    protected override ILimiterHolderWithConfiguration<ConcurrencyLimiterOptions>  GetLimiter(string key)
    {
        return GrainFactory.GetConcurrencyLimiter(key);
    }
}