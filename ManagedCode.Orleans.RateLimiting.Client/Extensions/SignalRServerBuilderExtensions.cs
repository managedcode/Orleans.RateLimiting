using System;
using ManagedCode.Orleans.RateLimiting.Client.Middlewares;
using ManagedCode.Orleans.RateLimiting.Client.Options;
using ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ManagedCode.Orleans.RateLimiting.Client.Extensions;

public static class SignalRServerBuilderExtensions
{
    public static ISignalRServerBuilder AddOrleansRateLimiting(
        this ISignalRServerBuilder builder,
        string configurationName,
        RateLimitPartitionKind partitionKind = RateLimitPartitionKind.User)
    {
        builder.Services.AddOrleansRateLimiting(options =>
            options.AddToPolicy(SignalRRateLimitingDefaults.PolicyName, partitionKind, configurationName, required: true));
        builder.Services.Configure<SignalRRateLimitingOptions>(options =>
        {
            options.PolicyName = SignalRRateLimitingDefaults.PolicyName;
            options.ConfigurationName = configurationName;
            options.PartitionKind = partitionKind;
        });
        builder.Services.TryAddSingleton<RateLimitingHubFilter>();
        builder.Services.Configure<HubOptions>(options => options.AddFilter<RateLimitingHubFilter>());
        return builder;
    }

    public static ISignalRServerBuilder AddOrleansRateLimiting(this ISignalRServerBuilder builder, Action<SignalRRateLimitingOptions> configure)
    {
        var signalROptions = new SignalRRateLimitingOptions();
        configure(signalROptions);

        builder.Services.AddOrleansRateLimiting(options =>
            options.AddToPolicy(signalROptions.PolicyName, signalROptions.PartitionKind, signalROptions.ConfigurationName, required: true));
        builder.Services.Configure(configure);
        builder.Services.TryAddSingleton<RateLimitingHubFilter>();
        builder.Services.Configure<HubOptions>(options => options.AddFilter<RateLimitingHubFilter>());
        return builder;
    }
}
