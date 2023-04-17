using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Attributes;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster.Grains.Interfaces;

namespace ManagedCode.Orleans.RateLimiting.Tests.Cluster.Grains
{
    public class ConcurrencyLimiterGrain : Grain, IConcurrencyLimiterGrain
    {
        [ConcurrencyLimiter] //GrainId, default options  PermitLimit = 10; QueueLimit = 15;
        public Task<string> Do()
        {
            return Task.FromResult("Do");
        }

        [ConcurrencyLimiter(KeyType.Key, "go")] //GrainId, default options PermitLimit = 10; QueueLimit = 15;
        public Task<string> Go()
        {
            return Task.FromResult("Go");
        }

        [ConcurrencyLimiter(KeyType.GrainType, permitLimit:5, queueLimit:5)] 
        public Task<string> Take()
        {
            return Task.FromResult("Take");
        }
    }
}
