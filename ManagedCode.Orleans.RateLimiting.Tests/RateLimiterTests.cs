using System.Diagnostics;
using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;
using ManagedCode.TimeSeries.Summers;

namespace ManagedCode.Orleans.RateLimiting.Tests;

[ClassDataSource<TestClusterApplication>(Shared = SharedType.PerTestSession)]
public class RateLimiterTests
{
    private readonly TestClusterApplication _testApp;

    public RateLimiterTests(TestClusterApplication testApp)
    {
        _testApp = testApp;
    }

    [Test]
    public async Task GetConcurrencyLimiterTests()
    {
        var permit = 20;
        var extra = 5;
        var rateLimiter = _testApp.Cluster.Client.GetConcurrencyLimiter($"{nameof(GetConcurrencyLimiterTests)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new ConcurrencyLimiterOptions
        {
            PermitLimit = permit,
            QueueLimit = permit * 2,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });

        var errors = 0;
        var calls = 0;

        var token1 = new CancellationTokenSource();


        var tasks = Enumerable.Range(0, permit + extra).Select(s => Task.Run(async () =>
        {
            Interlocked.Increment(ref calls);
            await using var lease = await rateLimiter.AcquireAsync();
            if (lease.IsAcquired)
                do
                {
                    await Task.Delay(100);
                } while (!token1.Token.IsCancellationRequested);
            else
                Interlocked.Increment(ref errors);
        }));

        var run = Task.WhenAll(tasks);
        await Task.Delay(TimeSpan.FromSeconds(5));

        var statistics1 = await rateLimiter.GetStatisticsAsync();
        statistics1.ShouldNotBeNull();
        Console.WriteLine("TotalSuccessfulLeases " + statistics1.TotalSuccessfulLeases);
        Console.WriteLine("TotalFailedLeases " + statistics1.TotalFailedLeases);
        Console.WriteLine("CurrentAvailablePermits " + statistics1.CurrentAvailablePermits);
        Console.WriteLine("CurrentQueuedCount " + statistics1.CurrentQueuedCount);

        token1.Cancel();

        await Task.Delay(TimeSpan.FromSeconds(10));
        Console.WriteLine("------------------------");
        var statistics2 = await rateLimiter.GetStatisticsAsync();
        statistics2.ShouldNotBeNull();

        Console.WriteLine("TotalSuccessfulLeases " + statistics2.TotalSuccessfulLeases);
        Console.WriteLine("TotalFailedLeases " + statistics2.TotalFailedLeases);
        Console.WriteLine("CurrentAvailablePermits " + statistics2.CurrentAvailablePermits);
        Console.WriteLine("CurrentQueuedCount " + statistics2.CurrentQueuedCount);


        statistics1.TotalSuccessfulLeases.ShouldBe(permit);
        statistics1.CurrentQueuedCount.ShouldBe(extra);
        statistics1.CurrentAvailablePermits.ShouldBe(0);

        statistics2.TotalSuccessfulLeases.ShouldBe(permit + extra);
        statistics2.CurrentQueuedCount.ShouldBe(0);
        statistics2.CurrentAvailablePermits.ShouldBe(permit);
        await run;
    }

    [Test]
    public async Task GetFixedWindowRateLimiterTests()
    {
        var permit = 20;
        var summer = new IntTimeSeriesSummer(TimeSpan.FromSeconds(1));
        var rateLimiter = _testApp.Cluster.Client.GetFixedWindowRateLimiter($"{nameof(GetFixedWindowRateLimiterTests)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromSeconds(1),
            PermitLimit = permit,
            AutoReplenishment = true,
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });

        var errors = 0;
        var success = 0;
        var calls = 0;

        var sw = Stopwatch.StartNew();
        var tasks = Enumerable.Range(0, 1000).Select(s => Task.Run(async () =>
        {
            Interlocked.Increment(ref calls);
            await using (var lease = await rateLimiter.AcquireAsync())
            {
                if (lease.IsAcquired)
                {
                    Interlocked.Increment(ref success);
                    summer.Increment();
                }
                else
                {
                    Interlocked.Increment(ref errors);
                }
            }
        }));

        await Task.WhenAll(tasks);
        sw.Stop();
        var statistics = await rateLimiter.GetStatisticsAsync();
        statistics.ShouldNotBeNull();
        statistics.TotalSuccessfulLeases.ShouldBe(success);

        Console.WriteLine("Samples " + summer.Samples.Count);
        Console.WriteLine("TotalSuccessfulLeases " + statistics.TotalSuccessfulLeases);
        Console.WriteLine("TotalFailedLeases " + statistics.TotalFailedLeases);
        Console.WriteLine("CurrentAvailablePermits " + statistics.CurrentAvailablePermits);
        Console.WriteLine("CurrentQueuedCount " + statistics.CurrentQueuedCount);
        Console.WriteLine("Average " + summer.Average());


        foreach (var item in summer.Samples)
            Console.WriteLine(item.Key.ToString("O") + " " + item.Value);

        summer.Average().ShouldBeLessThanOrEqualTo(permit);
        summer.Samples.Values.ShouldAllBe(sample => sample <= permit);
    }

    [Test]
    public async Task GetSlidingWindowRateLimiterTests()
    {
        var permit = 20;
        var summer = new IntTimeSeriesSummer(TimeSpan.FromSeconds(1));
        var rateLimiter = _testApp.Cluster.Client.GetSlidingWindowRateLimiter($"{nameof(GetSlidingWindowRateLimiterTests)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new SlidingWindowRateLimiterOptions
        {
            Window = TimeSpan.FromSeconds(1),
            PermitLimit = permit,
            AutoReplenishment = true,
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            SegmentsPerWindow = 2
        });

        var errors = 0;
        var success = 0;
        var calls = 0;

        var sw = Stopwatch.StartNew();
        var tasks = Enumerable.Range(0, 1000).Select(s => Task.Run(async () =>
        {
            Interlocked.Increment(ref calls);
            await using (var lease = await rateLimiter.AcquireAsync())
            {
                if (lease.IsAcquired)
                {
                    Interlocked.Increment(ref success);
                    summer.Increment();
                }
                else
                {
                    Interlocked.Increment(ref errors);
                }
            }
        }));

        await Task.WhenAll(tasks);
        sw.Stop();
        var statistics = await rateLimiter.GetStatisticsAsync();
        statistics.ShouldNotBeNull();
        statistics.TotalSuccessfulLeases.ShouldBe(success);

        Console.WriteLine("Samples " + summer.Samples.Count);
        Console.WriteLine("TotalSuccessfulLeases " + statistics.TotalSuccessfulLeases);
        Console.WriteLine("TotalFailedLeases " + statistics.TotalFailedLeases);
        Console.WriteLine("CurrentAvailablePermits " + statistics.CurrentAvailablePermits);
        Console.WriteLine("CurrentQueuedCount " + statistics.CurrentQueuedCount);
        Console.WriteLine("Average " + summer.Average());


        foreach (var item in summer.Samples)
            Console.WriteLine(item.Key.ToString("O") + " " + item.Value);

        summer.Average().ShouldBeLessThanOrEqualTo(permit);
        summer.Samples.Values.ShouldAllBe(sample => sample <= permit);
    }

    [Test]
    public async Task TokenBucketRateLimiterTests()
    {
        var permit = 10;
        var summer = new IntTimeSeriesSummer(TimeSpan.FromSeconds(1));
        var rateLimiter = _testApp.Cluster.Client.GetTokenBucketRateLimiter($"{nameof(TokenBucketRateLimiterTests)}-{Guid.NewGuid():N}");
        await rateLimiter.Configure(new TokenBucketRateLimiterOptions
        {
            AutoReplenishment = true,
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            TokenLimit = permit,
            TokensPerPeriod = permit
        });

        var errors = 0;
        var success = 0;
        var calls = 0;

        var sw = Stopwatch.StartNew();
        var tasks = Enumerable.Range(0, 1000).Select(s => Task.Run(async () =>
        {
            Interlocked.Increment(ref calls);
            await using (var lease = await rateLimiter.AcquireAsync())
            {
                if (lease.IsAcquired)
                {
                    Interlocked.Increment(ref success);
                    summer.Increment();
                }
                else
                {
                    Interlocked.Increment(ref errors);
                }
            }
        }));

        await Task.WhenAll(tasks);
        sw.Stop();
        var statistics = await rateLimiter.GetStatisticsAsync();
        statistics.ShouldNotBeNull();
        statistics.TotalSuccessfulLeases.ShouldBe(success);

        Console.WriteLine("Samples " + summer.Samples.Count);
        Console.WriteLine("TotalSuccessfulLeases " + statistics.TotalSuccessfulLeases);
        Console.WriteLine("TotalFailedLeases " + statistics.TotalFailedLeases);
        Console.WriteLine("CurrentAvailablePermits " + statistics.CurrentAvailablePermits);
        Console.WriteLine("CurrentQueuedCount " + statistics.CurrentQueuedCount);
        Console.WriteLine("Average " + summer.Average());


        foreach (var item in summer.Samples)
            Console.WriteLine(item.Key.ToString("O") + " " + item.Value);

        summer.Average().ShouldBeLessThanOrEqualTo(permit);
        summer.Samples.Values.ShouldAllBe(sample => sample <= permit);
    }
}
