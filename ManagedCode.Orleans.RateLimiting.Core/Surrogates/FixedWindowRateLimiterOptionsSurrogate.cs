using System;
using System.Threading.RateLimiting;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Surrogates;

[Immutable]
[GenerateSerializer]
public struct FixedWindowRateLimiterOptionsSurrogate
{
    public FixedWindowRateLimiterOptionsSurrogate()
    {
        PermitLimit = 0;
        QueueLimit = 0;
    }

    [Id(0)] public TimeSpan Window = TimeSpan.Zero;

    [Id(1)] public bool AutoReplenishment = true;

    [Id(2)] public int PermitLimit;

    [Id(3)] public QueueProcessingOrder QueueProcessingOrder = QueueProcessingOrder.OldestFirst;

    [Id(4)] public int QueueLimit;
}