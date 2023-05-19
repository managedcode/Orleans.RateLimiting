using System;

namespace ManagedCode.Orleans.RateLimiting.Client.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class IpRateLimiterAttribute : Attribute, IRateLimiterAttribute
{
    public string ConfigurationName { get; }
    
    public IpRateLimiterAttribute(string configurationName)
    {
        ConfigurationName = configurationName;
    }
}