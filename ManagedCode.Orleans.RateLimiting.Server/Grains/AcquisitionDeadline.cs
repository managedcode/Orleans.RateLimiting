using System;
using System.Threading;

namespace ManagedCode.Orleans.RateLimiting.Server.Grains;

internal readonly struct AcquisitionDeadline
{
    private const string TimeoutMessage = "The rate limiter acquisition deadline expired.";
    private static readonly TimeSpan MaximumWait = TimeSpan.FromMilliseconds(int.MaxValue);
    private readonly TimeProvider? _clock;
    private readonly TimeSpan _timeout;
    private readonly long _started;
    internal bool IsEnabled => _clock is not null;

    internal AcquisitionDeadline(TimeSpan timeout, TimeProvider clock)
    {
        if (timeout == Timeout.InfiniteTimeSpan)
            return;

        ArgumentOutOfRangeException.ThrowIfLessThan(timeout, TimeSpan.Zero);
        _clock = clock;
        _timeout = timeout;
        _started = clock.GetTimestamp();
    }

    private TimeSpan Remaining
    {
        get
        {
            if (_clock is null) return Timeout.InfiniteTimeSpan;
            var remaining = _timeout - _clock.GetElapsedTime(_started);
            return remaining <= TimeSpan.Zero ? TimeSpan.Zero : remaining < MaximumWait ? remaining : MaximumWait;
        }
    }

    internal bool IsExpired => IsEnabled && Remaining == TimeSpan.Zero;

    internal void ThrowIfExpired()
    {
        if (IsExpired) throw CreateException();
    }

    internal AcquisitionCancellation CreateCancellation(CancellationToken callerToken)
    {
        if (_clock is null)
            return new AcquisitionCancellation(callerToken);

        callerToken.ThrowIfCancellationRequested();
        var remaining = Remaining;
        if (remaining == TimeSpan.Zero)
            throw CreateException();

        return new AcquisitionCancellation(new CancellationTokenSource(remaining, _clock), callerToken);
    }

    internal static TimeoutException CreateException() => new(TimeoutMessage);
}
