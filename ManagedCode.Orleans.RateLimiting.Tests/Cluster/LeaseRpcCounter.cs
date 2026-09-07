using ManagedCode.Orleans.RateLimiting.Core.Interfaces;

namespace ManagedCode.Orleans.RateLimiting.Tests.Cluster;

public sealed class LeaseRpcCounter : IOutgoingGrainCallFilter
{
    private int _releases;
    private int _bounded;
    private int _cancellable;
    public int BoundedCalls => Volatile.Read(ref _bounded);
    public int CancellableCalls => Volatile.Read(ref _cancellable);
    public int ReleaseCalls => Volatile.Read(ref _releases);
    public void Reset()
    {
        Interlocked.Exchange(ref _releases, default);
        Interlocked.Exchange(ref _bounded, default);
        Interlocked.Exchange(ref _cancellable, default);
    }

    public async Task Invoke(IOutgoingGrainCallContext context)
    {
        if (context.MethodName == nameof(IRateLimiterGrain.ReleaseLease))
            Interlocked.Increment(ref _releases);
        if (context.MethodName is nameof(IBoundedRateLimiterGrain.AcquireWithDeadlineAsync)
            or nameof(IBoundedRateLimiterGrain<object>.AcquireAndCheckConfigurationWithDeadlineAsync))
            Interlocked.Increment(ref _bounded);
        if (context.MethodName is nameof(IBoundedRateLimiterGrain.AcquireCancellableWithDeadlineAsync)
            or nameof(IBoundedRateLimiterGrain<object>.AcquireAndCheckConfigurationCancellableWithDeadlineAsync))
            Interlocked.Increment(ref _cancellable);
        await context.Invoke();
    }
}
