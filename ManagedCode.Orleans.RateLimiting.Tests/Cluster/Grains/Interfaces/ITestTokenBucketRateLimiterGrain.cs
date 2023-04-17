namespace ManagedCode.Orleans.RateLimiting.Tests.Cluster.Grains.Interfaces;

public interface ITestTokenBucketRateLimiterGrain : IGrainWithStringKey
{
    Task<string> Do();
    Task<string> Go();
    Task<string> Take();
}