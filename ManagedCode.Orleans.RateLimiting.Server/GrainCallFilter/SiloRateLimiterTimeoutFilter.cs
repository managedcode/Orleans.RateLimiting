using ManagedCode.Orleans.RateLimiting.Core.Services;
using Microsoft.Extensions.Options;
using Orleans.Configuration;

namespace ManagedCode.Orleans.RateLimiting.Server.GrainCallFilter;

internal sealed class SiloRateLimiterTimeoutFilter(IOptions<SiloMessagingOptions> options)
    : RateLimiterTimeoutFilter(() => options.Value.ResponseTimeout);
