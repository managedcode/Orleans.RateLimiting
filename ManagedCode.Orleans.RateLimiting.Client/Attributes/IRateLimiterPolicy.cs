namespace ManagedCode.Orleans.RateLimiting.Client.Attributes;

public interface IRateLimiterPolicy
{
    string ConfigurationName { get; }
}
