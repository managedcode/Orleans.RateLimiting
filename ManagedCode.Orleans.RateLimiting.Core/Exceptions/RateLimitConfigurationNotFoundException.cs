using System;

namespace ManagedCode.Orleans.RateLimiting.Core.Exceptions;

public sealed class RateLimitConfigurationNotFoundException : Exception
{
    public RateLimitConfigurationNotFoundException()
    {
    }

    public RateLimitConfigurationNotFoundException(string configurationName)
        : base(RateLimiterExceptionMessages.ConfigurationNotFound(configurationName))
    {
        ConfigurationName = configurationName;
    }

    public string? ConfigurationName { get; }
}
