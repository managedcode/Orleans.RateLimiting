using System;

namespace ManagedCode.Orleans.RateLimiting.Client.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AnonymousIpRateLimiterAttribute : Attribute, IRateLimiterAttribute
{
    public string ConfigurationName { get; }
    
    public AnonymousIpRateLimiterAttribute(string configurationName)
    {
        ConfigurationName = configurationName;
    }
}