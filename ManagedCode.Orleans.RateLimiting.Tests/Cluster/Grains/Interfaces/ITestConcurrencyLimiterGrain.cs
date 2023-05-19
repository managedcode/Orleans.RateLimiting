namespace ManagedCode.Orleans.RateLimiting.Tests.Cluster.Grains.Interfaces
{
    public interface ITestConcurrencyLimiterGrain : IGrainWithStringKey
    {
        Task<string> Do();
        Task<string> Go();
        Task<string> Take();
        Task<string> Skip();
    }
}
