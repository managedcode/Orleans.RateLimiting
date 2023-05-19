using System;

namespace ManagedCode.Orleans.RateLimiting.Client.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizedIpRateLimiterAttribute : Attribute, IRateLimiterAttribute
{
    public string ConfigurationName { get; }
    
    public AuthorizedIpRateLimiterAttribute(string configurationName)
    {
        ConfigurationName = configurationName;
    }
}


[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class InRoleIpRateLimiterAttribute : Attribute, IRateLimiterAttribute
{
    public string ConfigurationName { get; }
    public string Role { get; }

    public InRoleIpRateLimiterAttribute(string configurationName, string role)
    {
        ConfigurationName = configurationName;
        Role = role;
    }
}