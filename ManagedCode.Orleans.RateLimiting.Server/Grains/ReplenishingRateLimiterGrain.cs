namespace ManagedCode.Orleans.RateLimiting.Server.Grains;

// public class ReplenishingRateLimiterGrain : Grain, IReplenishingRateLimiterGrain
// {
//     private readonly ReplenishingRateLimiter _replenishingRateLimiter;
//     public async ValueTask<OrleansRateLimitLease> AcquireAsync(int permitCount = 1, CancellationToken cancellationToken = default)
//     {
//         var lease = await _replenishingRateLimiter.AcquireAsync(permitCount, cancellationToken);
//         throw new Exception();
//         
//     }
// }