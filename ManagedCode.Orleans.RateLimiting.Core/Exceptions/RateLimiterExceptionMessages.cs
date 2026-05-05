namespace ManagedCode.Orleans.RateLimiting.Core.Exceptions;

public static class RateLimiterExceptionMessages
{
    private const string ConfigurationNotFoundPrefix = "Rate limiter configuration '";
    private const string PartitionKeyNotFoundPrefix = "Rate limit partition key for '";
    private const string NotFoundSuffix = "' was not found.";

    public static string ConfigurationNotFound(string configurationName)
    {
        return string.Concat(ConfigurationNotFoundPrefix, configurationName, NotFoundSuffix);
    }

    public static string PartitionKeyNotFound(object kind)
    {
        return string.Concat(PartitionKeyNotFoundPrefix, kind, NotFoundSuffix);
    }
}
