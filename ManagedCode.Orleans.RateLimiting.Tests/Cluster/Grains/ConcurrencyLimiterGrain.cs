using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Attributes;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster.Grains.Interfaces;

namespace ManagedCode.Orleans.RateLimiting.Tests.Cluster.Grains
{
    public class TestConcurrencyLimiterGrain : Grain, ITestConcurrencyLimiterGrain
    {
        [ConcurrencyLimiter] //GrainId, default options  PermitLimit = 10; QueueLimit = 15;
        public async Task<string> Do()
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            return "Do";
        }

        [ConcurrencyLimiter(KeyType.Key, "go")] //Key, default options PermitLimit = 10; QueueLimit = 15;
        public async Task<string> Go()
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            return "Go";
        }

        [ConcurrencyLimiter(KeyType.GrainType, permitLimit:2, queueLimit:1)] 
        public async Task<string> Take()
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            return "Take";
        }
    }
}
