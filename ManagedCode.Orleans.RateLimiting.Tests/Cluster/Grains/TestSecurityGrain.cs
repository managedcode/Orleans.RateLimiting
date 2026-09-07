using ManagedCode.Orleans.RateLimiting.Core.Attributes;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster.Grains.Interfaces;

namespace ManagedCode.Orleans.RateLimiting.Tests.Cluster.Grains;

public sealed class TestSecurityGrain : Grain, ITestSecurityGrain
{
    private int _calls;

    [FixedWindowRateLimiter(SecurityGrainOptions.Strict)]
    public Task<int> NamedStrictFixed() => CountCall();

    [FixedWindowRateLimiter(SecurityGrainOptions.Loose)]
    public Task<int> NamedLooseFixed() => CountCall();

    [FixedWindowRateLimiter(windowInSeconds: SecurityGrainOptions.WindowSeconds, permitLimit: SecurityGrainOptions.StrictPermits)]
    public Task<int> InlineStrictFixed() => CountCall();

    [FixedWindowRateLimiter(windowInSeconds: SecurityGrainOptions.WindowSeconds, permitLimit: SecurityGrainOptions.StrictPermits)]
    public Task<int> InlineStrictFixedAlias() => CountCall();

    [FixedWindowRateLimiter(windowInSeconds: SecurityGrainOptions.WindowSeconds, permitLimit: SecurityGrainOptions.LoosePermits)]
    public Task<int> InlineLooseFixed() => CountCall();

    [FixedWindowRateLimiter(KeyType.Key)]
    public Task<int> MissingKeyFixed() => CountCall();

    [FixedWindowRateLimiter(SecurityGrainOptions.EmptyConfiguration)]
    public Task<int> EmptyConfigurationFixed() => CountCall();

    public Task<int> InterfaceFixed() => CountCall();

    [SlidingWindowRateLimiter(SecurityGrainOptions.Strict)]
    public Task<int> NamedStrictSliding() => CountCall();

    [SlidingWindowRateLimiter(SecurityGrainOptions.Loose)]
    public Task<int> NamedLooseSliding() => CountCall();

    [SlidingWindowRateLimiter(windowinSeconds: SecurityGrainOptions.WindowSeconds, permitLimit: SecurityGrainOptions.StrictPermits, segmentsPerWindow: SecurityGrainOptions.Segments)]
    public Task<int> InlineStrictSliding() => CountCall();

    [SlidingWindowRateLimiter(windowinSeconds: SecurityGrainOptions.WindowSeconds, permitLimit: SecurityGrainOptions.LoosePermits, segmentsPerWindow: SecurityGrainOptions.Segments)]
    public Task<int> InlineLooseSliding() => CountCall();

    [SlidingWindowRateLimiter(KeyType.Key)]
    public Task<int> MissingKeySliding() => CountCall();

    [SlidingWindowRateLimiter(SecurityGrainOptions.EmptyConfiguration)]
    public Task<int> EmptyConfigurationSliding() => CountCall();

    public Task<int> InterfaceSliding() => CountCall();

    [TokenBucketRateLimiter(SecurityGrainOptions.Strict)]
    public Task<int> NamedStrictToken() => CountCall();

    [TokenBucketRateLimiter(SecurityGrainOptions.Loose)]
    public Task<int> NamedLooseToken() => CountCall();

    [TokenBucketRateLimiter(replenishmentPeriodInSeconds: SecurityGrainOptions.WindowSeconds, tokenLimit: SecurityGrainOptions.StrictPermits, tokensPerPeriod: SecurityGrainOptions.StrictPermits)]
    public Task<int> InlineStrictToken() => CountCall();

    [TokenBucketRateLimiter(replenishmentPeriodInSeconds: SecurityGrainOptions.WindowSeconds, tokenLimit: SecurityGrainOptions.LoosePermits, tokensPerPeriod: SecurityGrainOptions.LoosePermits)]
    public Task<int> InlineLooseToken() => CountCall();

    [TokenBucketRateLimiter(KeyType.Key)]
    public Task<int> MissingKeyToken() => CountCall();

    [TokenBucketRateLimiter(SecurityGrainOptions.EmptyConfiguration)]
    public Task<int> EmptyConfigurationToken() => CountCall();

    public Task<int> InterfaceToken() => CountCall();

    [ConcurrencyLimiter(SecurityGrainOptions.Strict, key: SecurityGrainOptions.ConcurrencyKey)]
    public Task<int> NamedStrictConcurrency() => CountCall();

    [ConcurrencyLimiter(SecurityGrainOptions.Loose, key: SecurityGrainOptions.ConcurrencyKey)]
    public Task<int> NamedLooseConcurrency() => CountCall();

    [ConcurrencyLimiter(key: SecurityGrainOptions.InlineConcurrencyKey, permitLimit: SecurityGrainOptions.StrictPermits)]
    public Task<int> InlineStrictConcurrency() => CountCall();

    [ConcurrencyLimiter(key: SecurityGrainOptions.InlineConcurrencyKey, permitLimit: SecurityGrainOptions.LoosePermits)]
    public Task<int> InlineLooseConcurrency() => CountCall();

    [ConcurrencyLimiter(KeyType.Key)]
    public Task<int> MissingKeyConcurrency() => CountCall();

    [ConcurrencyLimiter(SecurityGrainOptions.EmptyConfiguration)]
    public Task<int> EmptyConfigurationConcurrency() => CountCall();

    public Task<int> InterfaceConcurrency() => Peer().InterfaceConcurrencyProbe();

    public Task<int> InterfaceConcurrencyProbe() => CountCall();

    [ConcurrencyLimiter(SecurityGrainOptions.Strict, key: SecurityGrainOptions.ConcurrencyKey)]
    public async Task<int> NestedNamedConcurrency()
    {
        var peer = Peer();
        await peer.NamedLooseConcurrency();
        return await peer.NamedStrictConcurrency();
    }

    [ConcurrencyLimiter(key: SecurityGrainOptions.InlineConcurrencyKey, permitLimit: SecurityGrainOptions.StrictPermits)]
    public async Task<int> NestedInlineConcurrency()
    {
        var peer = Peer();
        await peer.InlineLooseConcurrency();
        return await peer.InlineStrictConcurrency();
    }

    private ITestSecurityGrain Peer() => GrainFactory.GetGrain<ITestSecurityGrain>(Guid.NewGuid().ToString());

    private Task<int> CountCall() => Task.FromResult(++_calls);
}
