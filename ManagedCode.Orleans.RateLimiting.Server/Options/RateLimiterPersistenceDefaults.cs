namespace ManagedCode.Orleans.RateLimiting.Server.Options;

public static class RateLimiterPersistenceDefaults
{
    public const int StateFlushPeriodMinutes = 5;
    public const string StateName = ManagedCode.Orleans.RateLimiting.Server.RateLimiterStorageNames.StateName;
    public const string StorageProviderName = ManagedCode.Orleans.RateLimiting.Server.RateLimiterStorageNames.StorageProviderName;
}
