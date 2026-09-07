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
    private const string InvalidKeyMessage = "A rate limiter attribute must specify a valid key type and a non-empty explicit key.";

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

        if (context.InterfaceMethod is { } interfaceMethod && Attribute.IsDefined(interfaceMethod, typeof(TAttribute)))
            return CreateRateLimiter(context, Attribute.GetCustomAttribute(interfaceMethod, typeof(TAttribute)));

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

        var key = limiterAttribute.KeyType switch
        {
            KeyType.Key when !string.IsNullOrWhiteSpace(limiterAttribute.Key) => limiterAttribute.Key,
            KeyType.GrainType => context.ImplementationMethod.DeclaringType?.FullName ?? context.TargetContext.GrainId.ToString(),
            KeyType.GrainId => context.TargetContext.GrainId.ToString(),
            _ => throw new InvalidOperationException(InvalidKeyMessage)
        };

        if (limiterAttribute.ConfigurationName is not null)
        {
            var options = RateLimiterConfigs.FirstOrDefault(f => f.NameEquals(limiterAttribute.ConfigurationName) && f.OptionsTypeIs<TOptions>());
            if (options is not null)
                return (GetLimiter(RateLimiterPolicyKeys.ForNamedPolicy(key, options.Name)), options.GetLimiterOptions<TOptions>()!);

            throw new RateLimitConfigurationNotFoundException(limiterAttribute.ConfigurationName);
        }

        return (GetLimiter(RateLimiterPolicyKeys.ForInlinePolicy(key, limiterAttribute.Options)), limiterAttribute.Options);
    }

    protected abstract ILimiterHolderWithConfiguration<TOptions> GetLimiter(string key);
}
