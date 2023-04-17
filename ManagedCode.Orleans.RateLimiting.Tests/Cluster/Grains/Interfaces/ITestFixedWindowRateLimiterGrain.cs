namespace ManagedCode.Orleans.RateLimiting.Tests.Cluster.Grains.Interfaces;

public interface ITestFixedWindowRateLimiterGrain : IGrainWithStringKey
{
    Task<string> Do();
    Task<string> Go();
    Task<string> Take();
}