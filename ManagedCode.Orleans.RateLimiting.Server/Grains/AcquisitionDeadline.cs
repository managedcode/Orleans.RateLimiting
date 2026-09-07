using System;
using System.Diagnostics;
using System.Threading;

namespace ManagedCode.Orleans.RateLimiting.Server.Grains;

internal readonly struct AcquisitionDeadline
{
    private const string TimeoutMessage = "The rate limiter acquisition deadline expired.";
    private static readonly TimeSpan MaximumWait = TimeSpan.FromMilliseconds(int.MaxValue);
    private readonly TimeSpan _timeout;
    private readonly long _started;
    internal bool IsEnabled { get; }

    internal AcquisitionDeadline(TimeSpan timeout)
    {
        if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
            throw new ArgumentOutOfRangeException(nameof(timeout));
        _timeout = timeout;
        _started = Stopwatch.GetTimestamp();
        IsEnabled = timeout != Timeout.InfiniteTimeSpan;
    }

    internal TimeSpan Remaining
    {
        get
        {
            if (!IsEnabled) return Timeout.InfiniteTimeSpan;
            var remaining = _timeout - Stopwatch.GetElapsedTime(_started);
            return remaining <= TimeSpan.Zero ? TimeSpan.Zero : remaining < MaximumWait ? remaining : MaximumWait;
        }
    }

    internal bool IsExpired => IsEnabled && Remaining == TimeSpan.Zero;

    internal void ThrowIfExpired()
    {
        if (IsExpired) throw CreateException();
    }

    internal CancellationTokenSource CreateCancellationSource(CancellationToken callerToken)
    {
        var source = CancellationTokenSource.CreateLinkedTokenSource(callerToken);
        source.CancelAfter(Remaining);
        return source;
    }

    internal static TimeoutException CreateException() => new(TimeoutMessage);
}
