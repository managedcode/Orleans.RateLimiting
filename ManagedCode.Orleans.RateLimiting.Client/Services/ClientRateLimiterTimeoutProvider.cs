using ManagedCode.Orleans.RateLimiting.Core.Services;
using Microsoft.Extensions.Options;
using Orleans.Configuration;

namespace ManagedCode.Orleans.RateLimiting.Client.Services;

internal sealed class ClientRateLimiterTimeoutProvider(IOptions<ClientMessagingOptions> options)
    : RateLimiterTimeoutProvider(() => options.Value.ResponseTimeout);
