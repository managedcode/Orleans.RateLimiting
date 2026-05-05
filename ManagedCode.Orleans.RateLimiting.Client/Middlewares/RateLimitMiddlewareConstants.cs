namespace ManagedCode.Orleans.RateLimiting.Client.Middlewares;

internal static class RateLimitMiddlewareConstants
{
    public const string DefaultPath = "/";
    public const string UnknownAuthenticatedUserKey = "rate-user-name";
    public const string OperationNameSeparator = " ";
    public const string ResourceSeparator = ".";
    public const string TooManyRequestsError = "Too many requests";
    public const string ConfigurationNotFoundLogMessage = "Configuration {ConfigurationName} not found for RateLimiter";
    public const string GroupClaimType = "group";
    public const string GroupsClaimType = "groups";
    public const string TenantIdClaimType = "tenant_id";
    public const string ShortTenantIdClaimType = "tid";
    public const string HubMetadataKey = "hub";
    public const string MethodMetadataKey = "method";
    public const string ConfigurationMetadataKey = "configuration";
    public const string PartitionMetadataKey = "partition";
    public const string PathMetadataKey = "path";
    public const string SignalRRateLimitedLogMessage = "SignalR invocation {HubMethodName} was rate limited: {Reason}";
}
