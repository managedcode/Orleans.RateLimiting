using System;

namespace ManagedCode.Orleans.RateLimiting.Client.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AnonymousIpRateLimiterAttribute : Attribute, IRateLimiterAttribute
{
    public AnonymousIpRateLimiterAttribute(string configurationName)
    {
        ConfigurationName = configurationName;
    }

    public string ConfigurationName { get; }
}