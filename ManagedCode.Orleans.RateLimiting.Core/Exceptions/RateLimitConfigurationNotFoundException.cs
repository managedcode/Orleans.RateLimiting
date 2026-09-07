using System;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Core.Exceptions;

[GenerateSerializer]
public sealed class RateLimitConfigurationNotFoundException : Exception
{
    private const int ConfigurationNameFieldId = 0;

    public RateLimitConfigurationNotFoundException()
    {
    }

    public RateLimitConfigurationNotFoundException(string configurationName)
        : base(RateLimiterExceptionMessages.ConfigurationNotFound(configurationName))
    {
        ConfigurationName = configurationName;
    }

    [Id(ConfigurationNameFieldId)]
    public string? ConfigurationName { get; }
}
