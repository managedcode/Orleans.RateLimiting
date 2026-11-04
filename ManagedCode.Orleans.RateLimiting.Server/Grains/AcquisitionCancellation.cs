using System;
using System.Threading;

namespace ManagedCode.Orleans.RateLimiting.Server.Grains;

internal readonly struct AcquisitionCancellation : IDisposable
{
    private readonly CancellationTokenSource? _timeout;
    private readonly CancellationTokenSource? _linked;

    internal AcquisitionCancellation(CancellationToken callerToken) => Token = callerToken;

    internal AcquisitionCancellation(CancellationTokenSource timeout, CancellationToken callerToken)
    {
        _timeout = timeout;
        if (callerToken.CanBeCanceled)
            _linked = CancellationTokenSource.CreateLinkedTokenSource(callerToken, _timeout.Token);

        Token = (_linked ?? _timeout).Token;
    }

    internal CancellationToken Token { get; }

    internal bool HasTimedOut => _timeout?.IsCancellationRequested == true;

    public void Dispose()
    {
        _linked?.Dispose();
        _timeout?.Dispose();
    }
}
