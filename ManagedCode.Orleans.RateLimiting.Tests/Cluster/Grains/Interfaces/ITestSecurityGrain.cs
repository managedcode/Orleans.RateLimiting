using ManagedCode.Orleans.RateLimiting.Core.Attributes;

namespace ManagedCode.Orleans.RateLimiting.Tests.Cluster.Grains.Interfaces;

public interface ITestSecurityGrain : IGrainWithStringKey
{
    Task<int> NamedStrictFixed();
    Task<int> NamedLooseFixed();
    Task<int> InlineStrictFixed();
    Task<int> InlineStrictFixedAlias();
    Task<int> InlineLooseFixed();
    Task<int> MissingKeyFixed();
    Task<int> EmptyConfigurationFixed();
    [FixedWindowRateLimiter(SecurityGrainOptions.Strict)]
    Task<int> InterfaceFixed();
    Task<int> NamedStrictSliding();
    Task<int> NamedLooseSliding();
    Task<int> InlineStrictSliding();
    Task<int> InlineLooseSliding();
    Task<int> MissingKeySliding();
    Task<int> EmptyConfigurationSliding();
    [SlidingWindowRateLimiter(SecurityGrainOptions.Strict)]
    Task<int> InterfaceSliding();
    Task<int> NamedStrictToken();
    Task<int> NamedLooseToken();
    Task<int> InlineStrictToken();
    Task<int> InlineLooseToken();
    Task<int> MissingKeyToken();
    Task<int> EmptyConfigurationToken();
    [TokenBucketRateLimiter(SecurityGrainOptions.Strict)]
    Task<int> InterfaceToken();
    Task<int> NamedStrictConcurrency();
    Task<int> NamedLooseConcurrency();
    Task<int> InlineStrictConcurrency();
    Task<int> InlineLooseConcurrency();
    Task<int> MissingKeyConcurrency();
    Task<int> EmptyConfigurationConcurrency();
    [ConcurrencyLimiter(SecurityGrainOptions.Strict, key: SecurityGrainOptions.InterfaceConcurrencyKey)]
    Task<int> InterfaceConcurrency();
    [ConcurrencyLimiter(SecurityGrainOptions.Strict, key: SecurityGrainOptions.InterfaceConcurrencyKey)]
    Task<int> InterfaceConcurrencyProbe();
    Task<int> NestedNamedConcurrency();
    Task<int> NestedInlineConcurrency();
}
