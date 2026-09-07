using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;

namespace ManagedCode.Orleans.RateLimiting.Tests;

[ClassDataSource<TimeoutTestCluster>(Shared = SharedType.PerTestSession)]
public class BoundedRpcSelectionTests(TimeoutTestCluster app)
{
    private const int PermitCount = 1;

    [Test]
    [Arguments(false, false)]
    [Arguments(false, true)]
    [Arguments(true, false)]
    [Arguments(true, true)]
    public async Task OnlyCancellableCallersUseDistributedCancellation(bool cancellable, bool configure)
    {
        var options = new ConcurrencyLimiterOptions { PermitLimit = PermitCount };
        var holder = app.Cluster.Client.GetConcurrencyLimiter(Guid.NewGuid().ToString(), options);
        await holder.Configure(options);
        using var cancellation = new CancellationTokenSource();
        var token = cancellable ? cancellation.Token : CancellationToken.None;
        app.RpcCounter.Reset();
        await using var lease = configure
            ? await holder.AcquireAndConfigureAsync(PermitCount, token)
            : await holder.AcquireAsync(PermitCount, token);
        lease.IsAcquired.ShouldBeTrue();
        app.RpcCounter.BoundedCalls.ShouldBe(cancellable ? default : PermitCount);
        app.RpcCounter.CancellableCalls.ShouldBe(cancellable ? PermitCount : default);
        await lease.DisposeAsync();
        (await holder.GetStatisticsAsync())!.CurrentAvailablePermits.ShouldBe(PermitCount);
        await holder.DeleteStateAsync();
    }
}
