using System;
using System.Threading.RateLimiting;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Surrogates;

[Immutable]
[GenerateSerializer]
public struct SlidingWindowRateLimiterOptionsSurrogate
{
    public SlidingWindowRateLimiterOptionsSurrogate()
    {
        SegmentsPerWindow = 0;
        PermitLimit = 0;
        QueueLimit = 0;
    }

    [Id(0)] public TimeSpan Window = TimeSpan.Zero;


    [Id(1)] public int SegmentsPerWindow;


    [Id(2)] public bool AutoReplenishment = true;


    [Id(3)] public int PermitLimit;


    [Id(4)] public QueueProcessingOrder QueueProcessingOrder = QueueProcessingOrder.OldestFirst;

    [Id(5)] public int QueueLimit;
}