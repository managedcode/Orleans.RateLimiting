namespace ManagedCode.Orleans.RateLimiting.Tests.Cluster.Grains.Interfaces
{
    public interface IConcurrencyLimiterGrain : IGrainWithStringKey
    {
        Task<string> Do();
        Task<string> Go();
        Task<string> Take();
    }
}
