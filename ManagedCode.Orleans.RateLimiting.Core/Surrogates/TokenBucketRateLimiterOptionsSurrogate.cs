using System;
using System.Threading.RateLimiting;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Surrogates;

[Immutable]
[GenerateSerializer]
public struct TokenBucketRateLimiterOptionsSurrogate
{
    public TokenBucketRateLimiterOptionsSurrogate()
    {
        TokensPerPeriod = 0;
        TokenLimit = 0;
        QueueLimit = 0;
    }

    [Id(0)]
    public TimeSpan ReplenishmentPeriod  = TimeSpan.Zero;


    [Id(1)] public int TokensPerPeriod;


    [Id(2)]
    public bool AutoReplenishment   = true;


    [Id(3)] public int TokenLimit;


    [Id(4)]
    public QueueProcessingOrder QueueProcessingOrder  = QueueProcessingOrder.OldestFirst;


    [Id(5)] public int QueueLimit;
}