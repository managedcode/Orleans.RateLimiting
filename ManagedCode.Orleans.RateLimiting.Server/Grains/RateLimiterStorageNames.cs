namespace ManagedCode.Orleans.RateLimiting.Server.Grains;

internal static class RateLimiterStorageNames
{
    public const string StateName = ManagedCode.Orleans.RateLimiting.Server.Options.RateLimiterPersistenceDefaults.StateName;
    public const string StorageProviderName = ManagedCode.Orleans.RateLimiting.Server.Options.RateLimiterPersistenceDefaults.StorageProviderName;
}
