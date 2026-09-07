using System;
using System.Threading;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using Orleans.Serialization.Invocation;

namespace ManagedCode.Orleans.RateLimiting.Core.Services;

/// <summary>Passes acquisition budgets to the silo without per-request timers or linked tokens.</summary>
public abstract class RateLimiterTimeoutProvider(Func<TimeSpan> responseTimeout)
{
    private const int AcquireTimeoutArgument = 1;
    private const int ConfigureTimeoutArgument = 2;
    private const int BudgetNumerator = 4;
    private const int BudgetDenominator = 5;

    internal void Apply(IInvokable request)
    {
        var timeout = request.GetDefaultResponseTimeout() ?? responseTimeout();
        var budget = timeout > TimeSpan.Zero ? TimeSpan.FromTicks(timeout.Ticks / BudgetDenominator * BudgetNumerator) : timeout;
        var index = request.GetMethodName() is nameof(IBoundedRateLimiterGrain.AcquireWithDeadlineAsync) or nameof(IBoundedRateLimiterGrain.AcquireCancellableWithDeadlineAsync)
            ? AcquireTimeoutArgument : ConfigureTimeoutArgument;
        var requested = (TimeSpan)request.GetArgument(index)!;
        if (requested != Timeout.InfiniteTimeSpan && (budget < TimeSpan.Zero || requested < budget))
            budget = requested;
        request.SetArgument(index, budget);
    }
}
