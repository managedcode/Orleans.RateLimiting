using System;

namespace ManagedCode.Orleans.RateLimiting.Server.Options;

public sealed class RateLimiterPersistenceOptions
{
    public TimeSpan StateFlushPeriod { get; set; } = TimeSpan.FromMinutes(RateLimiterPersistenceDefaults.StateFlushPeriodMinutes);
}
