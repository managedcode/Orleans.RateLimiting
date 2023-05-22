using System.Threading.RateLimiting;

namespace ManagedCode.Orleans.RateLimiting.Core.Interfaces;

public interface IConcurrencyLimiterGrain : IRateLimiterGrainWithConfiguration<ConcurrencyLimiterOptions>
{
}