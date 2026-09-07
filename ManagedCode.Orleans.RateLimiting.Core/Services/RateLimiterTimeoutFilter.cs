using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Services;

/// <summary>Passes acquisition budgets to the silo without per-request timers or linked tokens.</summary>
public abstract class RateLimiterTimeoutFilter(Func<TimeSpan> responseTimeout) : IOutgoingGrainCallFilter
{
    private const int AcquireTimeoutArgument = 1;
    private const int ConfigureTimeoutArgument = 2;
    private const int BudgetNumerator = 4;
    private const int BudgetDenominator = 5;

    public Task Invoke(IOutgoingGrainCallContext context)
    {
        if (typeof(IBoundedRateLimiterGrain).IsAssignableFrom(context.InterfaceMethod.DeclaringType))
        {
            var timeout = context.Request.GetDefaultResponseTimeout() ?? responseTimeout();
            var budget = timeout > TimeSpan.Zero ? TimeSpan.FromTicks(timeout.Ticks / BudgetDenominator * BudgetNumerator) : timeout;
            var index = context.MethodName is nameof(IBoundedRateLimiterGrain.AcquireWithDeadlineAsync) or nameof(IBoundedRateLimiterGrain.AcquireCancellableWithDeadlineAsync)
                ? AcquireTimeoutArgument : ConfigureTimeoutArgument;
            var requested = (TimeSpan)context.Request.GetArgument(index)!;
            if (requested != Timeout.InfiniteTimeSpan && (budget < TimeSpan.Zero || requested < budget))
                budget = requested;
            context.Request.SetArgument(index, budget);
        }
        return context.Invoke();
    }
}
