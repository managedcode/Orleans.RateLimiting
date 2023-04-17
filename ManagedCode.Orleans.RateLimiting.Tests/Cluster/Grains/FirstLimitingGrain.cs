using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Attributes;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster.Grains.Interfaces;

namespace ManagedCode.Orleans.RateLimiting.Tests.Cluster.Grains
{
    public class FirstLimitingGrain : Grain, IFirstLimitingGrain
    {
        [ConcurrencyLimiter] //GrainId, default options
        public Task<string> Do()
        {
            
            return Task.FromResult("Do");
        }

        [ConcurrencyLimiter(KeyType.Key, "go")] //GrainId, default options
        public Task<string> Go()
        {
            return Task.FromResult("Go");
        }

        public Task<string> Take()
        {
            return Task.FromResult("Take");
        }
    }
}
