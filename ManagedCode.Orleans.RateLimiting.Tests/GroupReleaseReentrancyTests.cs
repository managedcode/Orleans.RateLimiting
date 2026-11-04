using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Client.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;
using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;

namespace ManagedCode.Orleans.RateLimiting.Tests;

public class GroupReleaseReentrancyTests
{
    private const short SiloCount = 1;
    private const int PermitLimit = 1;
    private const int TimeoutSeconds = 10;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(TimeoutSeconds);

    [Test]
    public async Task DisposeFromReleaseFilterWaitsForPermitReturn()
    {
        var filter = new ReentrantReleaseFilter();
        var builder = new InProcessTestClusterBuilder(SiloCount);
        builder.ConfigureSilo((_, silo) => new TestSiloConfigurations().Configure(silo));
        builder.ConfigureClient(client => client.AddOrleansRateLimiting().Services.AddSingleton<IOutgoingGrainCallFilter>(filter));
        await using var cluster = builder.Build();
        await cluster.DeployAsync();

        var limiter = cluster.Client.GetConcurrencyLimiter(Guid.NewGuid().ToString(), new ConcurrencyLimiterOptions { PermitLimit = PermitLimit });
        await using var group = new GroupLimiterHolder();
        group.AddLimiter(limiter);
        (await group.AcquireAsync()).ShouldBeNull();
        filter.Reenter = () => group.DisposeAsync().AsTask();

        var first = group.DisposeAsync().AsTask();
        try
        {
            var nested = await filter.NestedDisposal.Task.WaitAsync(Timeout);
            nested.IsCompleted.ShouldBeFalse();
        }
        finally
        {
            filter.Continue.TrySetResult();
            await first.WaitAsync(Timeout);
        }

        await (await filter.NestedDisposal.Task).WaitAsync(Timeout);
        (await limiter.GetStatisticsAsync())!.CurrentAvailablePermits.ShouldBe(PermitLimit);
    }

    private sealed class ReentrantReleaseFilter : IOutgoingGrainCallFilter
    {
        public Func<Task>? Reenter { get; set; }
        public TaskCompletionSource<Task> NestedDisposal { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Continue { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task Invoke(IOutgoingGrainCallContext context)
        {
            if (context.MethodName == nameof(IRateLimiterGrain.ReleaseLease) && Reenter is { } reenter)
            {
                NestedDisposal.TrySetResult(reenter());
                await Continue.Task.WaitAsync(Timeout);
            }

            await context.Invoke();
        }
    }
}
