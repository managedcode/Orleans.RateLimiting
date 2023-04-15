using System.Threading.RateLimiting;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Surrogates;

[GenerateSerializer]
public struct ConcurrencyLimiterOptionsSurrogate
{
    public ConcurrencyLimiterOptionsSurrogate()
    {
        PermitLimit = 0;
        QueueLimit = 0;
    }

    [Id(0)]
    public int PermitLimit { get; set; }


    [Id(1)]
    public QueueProcessingOrder QueueProcessingOrder { get; set; } = QueueProcessingOrder.OldestFirst;


    [Id(2)]
    public int QueueLimit { get; set; }
}