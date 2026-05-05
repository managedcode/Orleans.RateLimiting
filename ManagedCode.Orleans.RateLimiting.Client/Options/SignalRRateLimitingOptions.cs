using ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;

namespace ManagedCode.Orleans.RateLimiting.Client.Options;

public sealed class SignalRRateLimitingOptions
{
    public string PolicyName { get; set; } = SignalRRateLimitingDefaults.PolicyName;

    public string ConfigurationName { get; set; } = SignalRRateLimitingDefaults.ConfigurationName;

    public RateLimitPartitionKind PartitionKind { get; set; } = RateLimitPartitionKind.User;

    public string AnonymousUserKey { get; set; } = SignalRRateLimitingDefaults.AnonymousUserKey;
}
