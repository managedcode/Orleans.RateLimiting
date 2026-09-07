using ManagedCode.Orleans.RateLimiting.Core.Services;
using Microsoft.Extensions.Options;
using Orleans.Configuration;

namespace ManagedCode.Orleans.RateLimiting.Server.Services;

internal sealed class SiloRateLimiterTimeoutProvider(IOptions<SiloMessagingOptions> options)
    : RateLimiterTimeoutProvider(() => options.Value.ResponseTimeout);
