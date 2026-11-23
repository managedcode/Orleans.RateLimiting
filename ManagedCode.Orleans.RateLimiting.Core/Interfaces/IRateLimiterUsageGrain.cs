using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Interfaces;

/// <summary>Returns a usage observation from the grain that replaced the configuration and reset the quota.</summary>
public interface IRateLimiterUsageGrain<TOptions> : IGrainWithStringKey where TOptions : class
{
    ValueTask<RateLimiterUsageSnapshot> ConfigureAndGetUsageAsync(TOptions options);
}
