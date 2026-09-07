using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime;
using Orleans.Serialization;

namespace ManagedCode.Orleans.RateLimiting.Tests.Performance;

internal static class PerformancePayload
{
    private const string LeaseIdText = "bb72c532-3c1a-4eae-ae52-679f952f527b";
    private const string GrainTypeName = "rate-limit-compatibility";
    private const string GrainKey = "lease-payload";

    internal static string Create()
    {
        var services = new ServiceCollection();
        services.AddSerializer(builder => builder.AddAssembly(typeof(RateLimitLeaseMetadata).Assembly));
        using var provider = services.BuildServiceProvider();
        using var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions { PermitLimit = PerformanceOptions.Single });
        using var lease = limiter.AttemptAcquire(PerformanceOptions.Single);
        var metadata = new RateLimitLeaseMetadata(Guid.Parse(LeaseIdText), GrainId.Create(GrainTypeName, GrainKey), lease);
        return Convert.ToBase64String(provider.GetRequiredService<Serializer>().SerializeToArray(metadata));
    }
}
