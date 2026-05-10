namespace ManagedCode.Orleans.RateLimiting.Core.Models;

public static class RateLimiterLogMessages
{
    private const string UnsupportedGrainTypePrefix = "Rate limiter grain type '";
    private const string UnsupportedGrainTypeSuffix = "' is not supported.";

    public const string ConfiguredLimiter = "Configured {LimiterType} with id:{GrainId}";
    public const string StateFlushFailed = "Failed to persist {LimiterType} state with id:{GrainId}";

    public static string UnsupportedGrainType(string? grainType)
    {
        return string.Concat(UnsupportedGrainTypePrefix, grainType, UnsupportedGrainTypeSuffix);
    }
}
