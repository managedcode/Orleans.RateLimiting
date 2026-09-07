using ManagedCode.Orleans.RateLimiting.Core.Services;
using Microsoft.Extensions.Options;
using Orleans.Configuration;

namespace ManagedCode.Orleans.RateLimiting.Client.GrainCallFilters;

internal sealed class ClientRateLimiterTimeoutFilter(IOptions<ClientMessagingOptions> options)
    : RateLimiterTimeoutFilter(() => options.Value.ResponseTimeout);
