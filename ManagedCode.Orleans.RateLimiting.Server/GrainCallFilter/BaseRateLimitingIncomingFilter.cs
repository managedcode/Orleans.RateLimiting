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
    protected readonly IGrainFactory GrainFactory;
    protected readonly IEnumerable<RateLimiterConfig> RateLimiterConfigs;

    protected BaseRateLimitingIncomingFilter(IGrainFactory grainFactory, IEnumerable<RateLimiterConfig> rateLimiterConfigs)
    {
        GrainFactory = grainFactory;
        RateLimiterConfigs = rateLimiterConfigs;
    }

    public async Task Invoke(IIncomingGrainCallContext context)
    {
        var limiter = IsRateLimiter(context);

        if (limiter.HasValue)
        {
            var (holder, options) = limiter.Value;
            await using var lease = options is null
                ? await holder.AcquireAsync()
                : await holder.AcquireAndCheckConfigurationAsync(options);
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

    private (ILimiterHolderWithConfiguration<TOptions> limiter, TOptions? options)? IsRateLimiter(IIncomingGrainCallContext context)
    {
        if (Attribute.IsDefined(context.ImplementationMethod, typeof(TAttribute)))
        {
            var attribute = Attribute.GetCustomAttribute(context.ImplementationMethod, typeof(TAttribute));
            return CreateRiteLimiter(context, attribute);
        }

        if (context.ImplementationMethod.DeclaringType != null && Attribute.IsDefined(context.ImplementationMethod.DeclaringType, typeof(TAttribute)))
        {
            var attribute = Attribute.GetCustomAttribute(context.ImplementationMethod.DeclaringType, typeof(TAttribute));
            return CreateRiteLimiter(context, attribute);
        }

        return null;
    }

    private (ILimiterHolderWithConfiguration<TOptions> limiter, TOptions? options)? CreateRiteLimiter(IIncomingGrainCallContext context, Attribute? attribute)
    {
        if (attribute is not ILimiterAttribute<TOptions> limiterAttribute)
            return null;

        var limiter = limiterAttribute.KeyType switch
        {
            KeyType.Key => string.IsNullOrWhiteSpace(limiterAttribute.Key) ? null : GetLimiter(limiterAttribute.Key),
            KeyType.GrainType => context.ImplementationMethod.DeclaringType?.FullName is { Length: > 0 } typeName ? GetLimiter(typeName) : null,
            KeyType.GrainId => GetLimiter(context.TargetContext.GrainId.ToString()),
            _ => null
        };

        if (limiter == null)
            return null;

        if (!string.IsNullOrEmpty(limiterAttribute.ConfigurationName))
        {
            var name = limiterAttribute.ConfigurationName.ToLowerInvariant();
            var options = RateLimiterConfigs.FirstOrDefault(f => f.Name == name && f.OptionsTypeIs<TOptions>());
            if (options is not null)
                return (limiter, options.GetLimiterOptions<TOptions>());
        }

        return (limiter, limiterAttribute.Options);
    }

    protected abstract ILimiterHolderWithConfiguration<TOptions> GetLimiter(string key);
}