namespace ManagedCode.Orleans.RateLimiting.Tests.Cluster.Grains.Interfaces;

public interface ITestFixedWindowRateLimiterGrain : IGrainWithStringKey
{
    Task<string> Run();
    Task<string> Go();
    Task<string> Take();
    Task<string> Skip();
}
