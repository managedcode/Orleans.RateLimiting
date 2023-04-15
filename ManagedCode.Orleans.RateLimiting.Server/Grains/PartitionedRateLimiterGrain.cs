namespace ManagedCode.Orleans.RateLimiting.Server.Grains;

// public class PartitionedRateLimiterGrain : Grain, IPartitionedRateLimiterGrain
// {
//      private readonly PartitionedRateLimiter _partitionedRateLimiter;
//      public async ValueTask<OrleansRateLimitLease> AcquireAsync(int permitCount = 1, CancellationToken cancellationToken = default)
//      {
//          PartitionedRateLimiter.
//          _partitionedRateLimiter.AcquireAsync(permitCount, cancellationToken);
//          throw new Exception();
//          
//      }
// }