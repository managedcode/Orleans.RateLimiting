namespace ManagedCode.Orleans.RateLimiting.Tests.Cluster.Grains.Interfaces;

public interface ITestSlidingWindowRateLimiterGrain : IGrainWithStringKey
{
    Task<string> Run();
    Task<string> Go();
    Task<string> Take();
    Task<string> Skip();
}
