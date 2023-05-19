using System.Threading.RateLimiting;

namespace ManagedCode.Orleans.RateLimiting.Core.Models;

public class RateLimiterConfig
{
    public string Name { get; private set; }
    public object Configuration { get; private set; }

    public RateLimiterConfig(string name, ConcurrencyLimiterOptions options)
    {
        Name = name.ToLowerInvariant();
        Configuration = options;
    }
    
    public RateLimiterConfig(string name, FixedWindowRateLimiterOptions options)
    {
        Name = name.ToLowerInvariant();
        Configuration = options;
    }
    
    public RateLimiterConfig(string name, SlidingWindowRateLimiterOptions options)
    {
        Name = name.ToLowerInvariant();
        Configuration = options;
    }
    
    public RateLimiterConfig(string name, TokenBucketRateLimiterOptions options)
    {
        Name = name.ToLowerInvariant();
        Configuration = options;
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