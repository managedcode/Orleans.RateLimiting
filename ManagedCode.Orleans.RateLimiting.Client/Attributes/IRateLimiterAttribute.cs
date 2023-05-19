namespace ManagedCode.Orleans.RateLimiting.Client.Attributes;

public interface IRateLimiterAttribute
{
    string ConfigurationName { get; }
}