using System;
using System.Threading.RateLimiting;

namespace ManagedCode.Orleans.RateLimiting.Core.Models;

public sealed class RateLimiterConfig
{
    public RateLimiterConfig(string name, ConcurrencyLimiterOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(options);

        Name = name;
        Configuration = options;
    }

    public RateLimiterConfig(string name, FixedWindowRateLimiterOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(options);

        Name = name;
        Configuration = options;
    }

    public RateLimiterConfig(string name, SlidingWindowRateLimiterOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(options);

        Name = name;
        Configuration = options;
    }

    public RateLimiterConfig(string name, TokenBucketRateLimiterOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(options);

        Name = name;
        Configuration = options;
    }

    public string Name { get; }
    public object Configuration { get; }

    public bool NameEquals(string name)
    {
        return string.Equals(Name, name, StringComparison.OrdinalIgnoreCase);
    }

    public T? GetLimiterOptions<T>() where T : class
    {
        return Configuration as T;
    }

    public bool OptionsTypeIs<T>()
    {
        return Configuration is T;
    }
}
