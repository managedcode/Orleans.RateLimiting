using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Attributes;
using ManagedCode.Orleans.RateLimiting.Core.Exceptions;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Server.GrainCallFilter;

public abstract class BaseRateLimitingIncomingFilter<TAttribute, TOptions> : IIncomingGrainCallFilter where TAttribute : Attribute where TOptions : class
{
    protected BaseRateLimitingIncomingFilter(IGrainFactory grainFactory, IEnumerable<RateLimiterConfig> rateLimiterConfigs)
    {
        GrainFactory = grainFactory;
        RateLimiterConfigs = rateLimiterConfigs;
    }

    protected IGrainFactory GrainFactory { get; }
    protected IEnumerable<RateLimiterConfig> RateLimiterConfigs { get; }

    public async Task Invoke(IIncomingGrainCallContext context)
    {
        var limiter = IsRateLimiter(context);

        if (limiter.HasValue)
        {
            await using var lease = await limiter.Value.Item1.AcquireAndCheckConfigurationAsync(limiter.Value.Item2);
            if (lease.IsAcquired)
                await context.Invoke();
            else
                throw new RateLimitExceededException(lease.Reason, lease.RetryAfter);
        }
        else
        {
            await context.Invoke();
        }
    }

    private (ILimiterHolderWithConfiguration<TOptions>, TOptions?)? IsRateLimiter(IIncomingGrainCallContext context)
    {
        if (Attribute.IsDefined(context.ImplementationMethod, typeof(TAttribute)))
        {
            var attribute = Attribute.GetCustomAttribute(context.ImplementationMethod, typeof(TAttribute));
            return CreateRateLimiter(context, attribute);
        }

        if (context.ImplementationMethod.DeclaringType != null && Attribute.IsDefined(context.ImplementationMethod.DeclaringType, typeof(TAttribute)))
        {
            var attribute = Attribute.GetCustomAttribute(context.ImplementationMethod.DeclaringType, typeof(TAttribute));
            return CreateRateLimiter(context, attribute);
        }

        return null;
    }

    private (ILimiterHolderWithConfiguration<TOptions>, TOptions?)? CreateRateLimiter(IIncomingGrainCallContext context, Attribute? attribute)
    {
        if (attribute is null)
            return null;

        var limiterAttribute = (ILimiterPolicy<TOptions>)attribute;

        var limiter = limiterAttribute.KeyType switch
        {
            KeyType.Key when !string.IsNullOrWhiteSpace(limiterAttribute.Key) => GetLimiter(limiterAttribute.Key),
            KeyType.GrainType => GetLimiter(context.ImplementationMethod.DeclaringType?.FullName ?? context.TargetContext.GrainId.ToString()),
            KeyType.GrainId => GetLimiter(context.TargetContext.GrainId.ToString()),
            _ => null
        };

        if (limiter == null)
            return null;

        if (!string.IsNullOrEmpty(limiterAttribute.ConfigurationName))
        {
            var options = RateLimiterConfigs.FirstOrDefault(f => f.NameEquals(limiterAttribute.ConfigurationName) && f.OptionsTypeIs<TOptions>());
            if (options is not null)
                return (limiter, options.GetLimiterOptions<TOptions>()!);

            throw new RateLimitConfigurationNotFoundException(limiterAttribute.ConfigurationName);
        }

        return (limiter, limiterAttribute.Options);
    }

    protected abstract ILimiterHolderWithConfiguration<TOptions> GetLimiter(string key);
}
