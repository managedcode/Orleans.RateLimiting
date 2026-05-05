using System;

namespace ManagedCode.Orleans.RateLimiting.Core.Exceptions;

public sealed class RateLimitConfigurationNotFoundException : Exception
{
    public RateLimitConfigurationNotFoundException()
    {
    }

    public RateLimitConfigurationNotFoundException(string configurationName)
        : base($"Rate limiter configuration '{configurationName}' was not found.")
    {
        ConfigurationName = configurationName;
    }

    public string? ConfigurationName { get; }
}
