using System;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using Microsoft.Extensions.Logging;
using Orleans;

namespace ManagedCode.Orleans.RateLimiting.Server.Grains;

public abstract partial class RateLimiterGrain<TLimiter, TOptions>
    where TLimiter : RateLimiter
    where TOptions : class
{
    private async Task ConfigureLimiterAsync(TOptions options, int? permitCount = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var replacement = CreateValidatedReplacement(options, permitCount);
        DisposeRateLimiter();
        _options = options;
        RateLimiter = replacement;
        await MutateStateAsync(state => ResetStateForConfiguration(state, options), flushImmediately: true, cancellationToken);
        _logger.LogInformation(RateLimiterLogMessages.ConfiguredLimiter, typeof(TLimiter).Name, this.GetPrimaryKeyString());
    }

    private TLimiter CreateValidatedReplacement(TOptions options, int? permitCount)
    {
        ArgumentNullException.ThrowIfNull(options);
        var previousOptions = _options;
        TLimiter? replacement = null;
        try
        {
            // The existing subclass factory reads Options. No await exposes these
            // candidate options to another Orleans turn before they are restored.
            _options = options;
            replacement = CreateDefaultRateLimiter();
            if (permitCount.HasValue)
                ArgumentOutOfRangeException.ThrowIfGreaterThan(permitCount.Value, PermitLimit, nameof(permitCount));
            return replacement;
        }
        catch
        {
            replacement?.Dispose();
            throw;
        }
        finally
        {
            _options = previousOptions;
        }
    }
}
