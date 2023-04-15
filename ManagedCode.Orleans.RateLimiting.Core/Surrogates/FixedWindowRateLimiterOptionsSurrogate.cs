using System;
using System.Threading.RateLimiting;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Surrogates;

[GenerateSerializer]
public struct FixedWindowRateLimiterOptionsSurrogate
{
    public FixedWindowRateLimiterOptionsSurrogate()
    {
        PermitLimit = 0;
        QueueLimit = 0;
    }

    [Id(0)]
    public TimeSpan Window { get; set; } = TimeSpan.Zero;

    [Id(1)]
    public bool AutoReplenishment { get; set; } = true;

    [Id(2)]
    public int PermitLimit { get; set; }

    [Id(3)]
    public QueueProcessingOrder QueueProcessingOrder { get; set; } = QueueProcessingOrder.OldestFirst;

    [Id(4)]
    public int QueueLimit { get; set; }
}