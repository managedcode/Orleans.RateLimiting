using ManagedCode.Orleans.RateLimiting.Core.Exceptions;
using ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace ManagedCode.Orleans.RateLimiting.Tests;

public class ExceptionSerializationSecurityTests
{
    private const string RejectionReason = "security-test-rejection";
    private const string ConfigurationName = "security-test-missing-policy";
    private const int RetrySeconds = 30;

    [Test]
    public void RateLimitExceptionRoundTripsAsExceptionWithReasonAndRetryAfter()
    {
        var original = new RateLimitExceededException(RejectionReason, TimeSpan.FromSeconds(RetrySeconds));
        var result = RoundTrip(original).ShouldBeOfType<RateLimitExceededException>();
        result.Message.ShouldBe(original.Message);
        result.Reason.ShouldBe(original.Reason);
        result.RetryAfter.ShouldBe(original.RetryAfter);
    }

    [Test]
    public void ConfigurationExceptionPreservesMissingName()
    {
        var original = new RateLimitConfigurationNotFoundException(ConfigurationName);
        var result = RoundTrip(original).ShouldBeOfType<RateLimitConfigurationNotFoundException>();
        result.Message.ShouldBe(original.Message);
        result.ConfigurationName.ShouldBe(ConfigurationName);
    }

    [Test]
    public void PartitionExceptionPreservesRequiredKind()
    {
        var original = new RateLimitPartitionKeyNotFoundException(RateLimitPartitionKind.Tenant);
        var result = RoundTrip(original).ShouldBeOfType<RateLimitPartitionKeyNotFoundException>();
        result.Message.ShouldBe(original.Message);
        result.Kind.ShouldBe(RateLimitPartitionKind.Tenant);
    }

    private static Exception RoundTrip(Exception exception)
    {
        using var services = new ServiceCollection().AddSerializer().BuildServiceProvider();
        var serializer = services.GetRequiredService<Serializer>();
        var result = serializer.Deserialize<Exception>(serializer.SerializeToArray(exception));
        result.ShouldNotBeNull();
        return result;
    }
}
