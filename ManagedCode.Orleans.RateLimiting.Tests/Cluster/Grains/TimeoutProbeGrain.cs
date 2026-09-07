using ManagedCode.Orleans.RateLimiting.Core.Extensions;

namespace ManagedCode.Orleans.RateLimiting.Tests.Cluster.Grains;

public interface ITimeoutProbeGrain : IGrainWithStringKey
{
    Task<bool> AcquireAsync(string limiterKey);
}

public class TimeoutProbeGrain : Grain, ITimeoutProbeGrain
{
    public async Task<bool> AcquireAsync(string limiterKey)
    {
        await using var lease = await GrainFactory.GetConcurrencyLimiter(limiterKey).AcquireAsync();
        return lease.IsAcquired;
    }
}
