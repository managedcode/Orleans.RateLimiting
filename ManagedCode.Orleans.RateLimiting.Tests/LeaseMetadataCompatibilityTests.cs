using ManagedCode.Orleans.RateLimiting.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace ManagedCode.Orleans.RateLimiting.Tests;

public class LeaseMetadataCompatibilityTests
{
    // Serialized by the unmodified 705b47d Core assembly, before field 4 existed.
    private const string PreviousPayload = "IEAhMsVyuxo8rk6uUmeflS9SeyEgIGCKc/8pQTFyYXRlLWxpbWl0LWNvbXBhdGliaWxpdHng4CFg+BZJ50EbbGVhc2UtcGF5bG9hZODgAQMh4OA=";
    private const string LeaseIdText = "bb72c532-3c1a-4eae-ae52-679f952f527b";

    [Test]
    public void PreviousLeasePayloadStillRequiresRelease()
    {
        using var services = new ServiceCollection().AddSerializer(builder => builder.AddAssembly(typeof(RateLimitLeaseMetadata).Assembly)).BuildServiceProvider();
        var metadata = services.GetRequiredService<Serializer>().Deserialize<RateLimitLeaseMetadata>(Convert.FromBase64String(PreviousPayload));
        metadata.ShouldNotBeNull();
        metadata.IsAcquired.ShouldBeTrue();
        metadata.LeaseId.ShouldBe(Guid.Parse(LeaseIdText));
        metadata.IsReleaseOptional.ShouldBeFalse();
    }
}
