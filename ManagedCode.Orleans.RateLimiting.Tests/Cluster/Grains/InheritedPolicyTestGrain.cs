using ManagedCode.Orleans.RateLimiting.Core.Attributes;

namespace ManagedCode.Orleans.RateLimiting.Tests.Cluster.Grains;

public interface IInheritedPolicyTestGrain : IGrainWithStringKey
{
    Task<int> PingAsync();
}

public abstract class InheritedPolicyBaseGrain : Grain
{
    private int _calls;

    public Task<int> PingAsync() => Task.FromResult(++_calls);
}

[FixedWindowRateLimiter(SecurityGrainOptions.Strict)]
public sealed class InheritedPolicyTestGrain : InheritedPolicyBaseGrain, IInheritedPolicyTestGrain
{
}
