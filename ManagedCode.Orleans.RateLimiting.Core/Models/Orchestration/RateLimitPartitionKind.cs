namespace ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;

public enum RateLimitPartitionKind
{
    User,
    Group,
    Tenant,
    Role,
    IpAddress,
    Endpoint,
    Grain,
    Custom
}
