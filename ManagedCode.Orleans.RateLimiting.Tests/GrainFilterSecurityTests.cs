using ManagedCode.Orleans.RateLimiting.Core.Exceptions;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster.Grains.Interfaces;

namespace ManagedCode.Orleans.RateLimiting.Tests;

[ClassDataSource<TestClusterApplication>(Shared = SharedType.PerTestSession)]
public class GrainFilterSecurityTests(TestClusterApplication testApp)
{
    [Test]
    [Arguments(Limiter.Fixed, false)]
    [Arguments(Limiter.Sliding, false)]
    [Arguments(Limiter.Token, false)]
    [Arguments(Limiter.Fixed, true)]
    [Arguments(Limiter.Sliding, true)]
    [Arguments(Limiter.Token, true)]
    public async Task AlternatingPoliciesCannotResetQuota(Limiter kind, bool inline)
    {
        var (strict, loose) = Resolve(CreateGrain(), kind, inline);
        await strict();
        await loose();
        await Should.ThrowAsync<RateLimitExceededException>(() => strict());
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task AlternateConcurrencyPolicyCannotReleaseAnActivePermit(bool inline)
    {
        var grain = CreateGrain();
        await Should.ThrowAsync<RateLimitExceededException>(() => inline
            ? grain.NestedInlineConcurrency() : grain.NestedNamedConcurrency());
    }

    [Test]
    public async Task MissingExplicitKeysFailClosedForEveryLimiter()
    {
        var grain = CreateGrain();
        Func<Task<int>>[] calls = [grain.MissingKeyFixed, grain.MissingKeySliding, grain.MissingKeyToken, grain.MissingKeyConcurrency];
        foreach (var call in calls)
            await Should.ThrowAsync<InvalidOperationException>(() => call());
    }

    [Test]
    public async Task EmptyNamedConfigurationsFailClosedForEveryLimiter()
    {
        var grain = CreateGrain();
        Func<Task<int>>[] calls = [grain.EmptyConfigurationFixed, grain.EmptyConfigurationSliding, grain.EmptyConfigurationToken, grain.EmptyConfigurationConcurrency];
        foreach (var call in calls)
            await Should.ThrowAsync<RateLimitConfigurationNotFoundException>(() => call());
    }

    [Test]
    public async Task InterfaceMethodQuotaAttributesAreEnforced()
    {
        var grain = CreateGrain();
        Func<Task<int>>[] calls = [grain.InterfaceFixed, grain.InterfaceSliding, grain.InterfaceToken];
        foreach (var call in calls)
        {
            await call();
            await Should.ThrowAsync<RateLimitExceededException>(() => call());
        }
    }

    [Test]
    public async Task InterfaceMethodConcurrencyAttributeIsEnforced()
    {
        await Should.ThrowAsync<RateLimitExceededException>(() => CreateGrain().InterfaceConcurrency());
    }

    [Test]
    public async Task IdenticalInlinePoliciesStillShareTheirIntendedQuota()
    {
        var grain = CreateGrain();
        await grain.InlineStrictFixed();
        await Should.ThrowAsync<RateLimitExceededException>(() => grain.InlineStrictFixedAlias());
    }

    private ITestSecurityGrain CreateGrain() => testApp.Cluster.Client.GetGrain<ITestSecurityGrain>(Guid.NewGuid().ToString());

    private static (Func<Task<int>> Strict, Func<Task<int>> Loose) Resolve(ITestSecurityGrain grain, Limiter kind, bool inline) => (kind, inline) switch
    {
        (Limiter.Fixed, false) => (grain.NamedStrictFixed, grain.NamedLooseFixed),
        (Limiter.Sliding, false) => (grain.NamedStrictSliding, grain.NamedLooseSliding),
        (Limiter.Token, false) => (grain.NamedStrictToken, grain.NamedLooseToken),
        (Limiter.Fixed, true) => (grain.InlineStrictFixed, grain.InlineLooseFixed),
        (Limiter.Sliding, true) => (grain.InlineStrictSliding, grain.InlineLooseSliding),
        (Limiter.Token, true) => (grain.InlineStrictToken, grain.InlineLooseToken),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public enum Limiter { Fixed, Sliding, Token }
}
