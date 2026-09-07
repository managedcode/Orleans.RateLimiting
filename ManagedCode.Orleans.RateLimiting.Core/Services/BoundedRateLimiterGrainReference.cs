using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Runtime;

namespace ManagedCode.Orleans.RateLimiting.Core.Services;

/// <summary>Supplies acquisition budgets before dispatch without a global call-filter pipeline.</summary>
[DefaultInvokableBaseType(typeof(Task<>), typeof(TaskRequest<>))]
public class BoundedRateLimiterGrainReference : GrainReference
{
    private readonly RateLimiterTimeoutProvider? _timeouts;

    protected BoundedRateLimiterGrainReference(GrainReferenceShared shared, IdSpan key) : base(shared, key)
        => _timeouts = shared.ServiceProvider.GetService<RateLimiterTimeoutProvider>();

    protected new ValueTask<T?> InvokeAsync<T>(IRequest request)
    {
        _timeouts?.Apply(request);
        return base.InvokeAsync<T>(request);
    }

    // Orleans validates both dispatch overloads even though bounded RPCs all return metadata.
    protected new ValueTask InvokeAsync(IRequest request) => base.InvokeAsync(request);
}
