using System;
using System.Threading.RateLimiting;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Surrogates;

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
    public TimeSpan ReplenishmentPeriod { get; set; } = TimeSpan.Zero;


    [Id(1)]
    public int TokensPerPeriod { get; set; }


    [Id(2)]
    public bool AutoReplenishment { get; set; } = true;


    [Id(3)]
    public int TokenLimit { get; set; }


    [Id(4)]
    public QueueProcessingOrder QueueProcessingOrder { get; set; } = QueueProcessingOrder.OldestFirst;


    [Id(5)]
    public int QueueLimit { get; set; }
}