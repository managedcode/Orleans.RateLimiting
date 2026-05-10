namespace ManagedCode.Orleans.RateLimiting.Server.Options;

public static class RateLimiterPersistenceDefaults
{
    public const int StateFlushPeriodMinutes = 5;
    public const string StateName = "rateLimiterState";
    public const string StorageProviderName = "ManagedCode.Orleans.RateLimiting";
}
