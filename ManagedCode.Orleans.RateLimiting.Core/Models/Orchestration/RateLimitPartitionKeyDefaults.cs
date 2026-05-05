namespace ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;

public static class RateLimitPartitionKeyDefaults
{
    public const string Separator = ":";
    public const string EscapeToken = "%";
    public const string EscapedEscapeToken = "%25";
    public const string EscapedSeparator = "%3A";
}
