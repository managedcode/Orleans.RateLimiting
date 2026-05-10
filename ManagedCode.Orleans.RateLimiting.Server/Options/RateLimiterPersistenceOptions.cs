using System;

namespace ManagedCode.Orleans.RateLimiting.Server.Options;

public sealed class RateLimiterPersistenceOptions
{
    private const int DefaultStateFlushPeriodMinutes = 5;

    public TimeSpan StateFlushPeriod { get; set; } = TimeSpan.FromMinutes(DefaultStateFlushPeriodMinutes);
}
