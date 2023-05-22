using System;

namespace ManagedCode.Orleans.RateLimiting.Client.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizedIpRateLimiterAttribute : Attribute, IRateLimiterAttribute
{
    public AuthorizedIpRateLimiterAttribute(string configurationName)
    {
        ConfigurationName = configurationName;
    }

    public string ConfigurationName { get; }
}