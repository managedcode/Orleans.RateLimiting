using ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;

namespace ManagedCode.Orleans.RateLimiting.Client.Options;

public sealed class SignalRRateLimitingOptions
{
    public string PolicyName { get; set; } = "SignalR";

    public string ConfigurationName { get; set; } = "SignalR";

    public RateLimitPartitionKind PartitionKind { get; set; } = RateLimitPartitionKind.User;

    public string AnonymousUserKey { get; set; } = "anonymous";
}
