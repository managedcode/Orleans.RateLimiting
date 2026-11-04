using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;
using ManagedCode.Orleans.RateLimiting.Core.Services;

namespace ManagedCode.Orleans.RateLimiting.Tests;

public class OrchestratorCancellationTests
{
    private const string ConfigurationName = "cancelled-policy";

    [Test]
    public async Task CancelledRequestCannotReturnAnEmptyGroupWithoutPolicies()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var orchestrator = new DefaultRateLimitRequestOrchestrator(null!, [], [], new DefaultRateLimitRequestKeyResolver());

        await AssertCancelledAsync(orchestrator, cancellation.Token);
    }

    [Test]
    public async Task PolicyReturningNoRulesAfterCancellationCannotReturnAGroup()
    {
        using var cancellation = new CancellationTokenSource();
        var policy = new TestPolicy(() =>
        {
            cancellation.Cancel();
            return [];
        });
        var orchestrator = new DefaultRateLimitRequestOrchestrator(null!, [], [policy], new DefaultRateLimitRequestKeyResolver());

        await AssertCancelledAsync(orchestrator, cancellation.Token);
    }

    [Test]
    public async Task ResolverReturningNoKeyAfterCancellationCannotReturnAGroup()
    {
        using var cancellation = new CancellationTokenSource();
        var policy = new TestPolicy(() => [new(RateLimitPartitionKind.User, ConfigurationName)]);
        var orchestrator = new DefaultRateLimitRequestOrchestrator(null!, [], [policy], new CancellingResolver(cancellation));

        await AssertCancelledAsync(orchestrator, cancellation.Token);
    }

    private static async Task AssertCancelledAsync(DefaultRateLimitRequestOrchestrator orchestrator, CancellationToken token)
    {
        var exception = await Should.ThrowAsync<OperationCanceledException>(() =>
            orchestrator.CreateLimiterGroupAsync(new RateLimitRequestContext(), token).AsTask());
        exception.CancellationToken.ShouldBe(token);
    }

    private sealed class TestPolicy(Func<IReadOnlyList<RateLimitRequestRule>> getRules) : IRateLimitRequestPolicy
    {
        public ValueTask<IReadOnlyList<RateLimitRequestRule>> GetRulesAsync(RateLimitRequestContext context, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(getRules());
    }

    private sealed class CancellingResolver(CancellationTokenSource cancellation) : IRateLimitRequestKeyResolver
    {
        public ValueTask<string?> ResolveKeyAsync(RateLimitRequestContext context, RateLimitRequestRule rule, CancellationToken cancellationToken = default)
        {
            cancellation.Cancel();
            return ValueTask.FromResult<string?>(null);
        }
    }
}
