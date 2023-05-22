using System;

namespace ManagedCode.Orleans.RateLimiting.Client.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class InRoleIpRateLimiterAttribute : Attribute, IRateLimiterAttribute
{
    public InRoleIpRateLimiterAttribute(string configurationName, string role)
    {
        ConfigurationName = configurationName;
        Role = role;
    }

    public string Role { get; }
    public string ConfigurationName { get; }
}