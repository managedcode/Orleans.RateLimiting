using System.Threading.RateLimiting;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Surrogates;

[Immutable]
[GenerateSerializer]
public struct ConcurrencyLimiterOptionsSurrogate
{
    public ConcurrencyLimiterOptionsSurrogate()
    {
        PermitLimit = 0;
        QueueLimit = 0;
    }

    [Id(0)] public int PermitLimit;


    [Id(1)] public QueueProcessingOrder QueueProcessingOrder = QueueProcessingOrder.OldestFirst;


    [Id(2)] public int QueueLimit;
}