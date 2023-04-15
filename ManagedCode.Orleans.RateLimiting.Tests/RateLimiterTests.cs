using System.Diagnostics;
using System.Threading.RateLimiting;
using FluentAssertions;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;
using ManagedCode.TimeSeries.Summers;
using Xunit;
using Xunit.Abstractions;

namespace ManagedCode.Orleans.RateLimiting.Tests;

[Collection(nameof(TestClusterApplication))]
public class RateLimiterTests
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly TestClusterApplication _testApp;

    public RateLimiterTests(TestClusterApplication testApp, ITestOutputHelper outputHelper)
    {
        _testApp = testApp;
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task GetConcurrencyLimiterTests()
    {
        var permit = 20;
        var extra = 5;
        var rateLimiter = _testApp.Cluster.Client.GetConcurrencyLimiter("test");
        await rateLimiter.Configure(new ConcurrencyLimiterOptions
        {
            PermitLimit = permit,
            QueueLimit = permit * 2,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });

        var errors = 0;
        var success = 0;
        var calls = 0;

        var token1 = new CancellationTokenSource();
        var token2 = new CancellationTokenSource();


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

        _ = Task.WhenAll(tasks);
        await Task.Delay(TimeSpan.FromSeconds(5));

        var statistics1 = await rateLimiter.GetStatisticsAsync();
        _outputHelper.WriteLine("TotalSuccessfulLeases " + statistics1.TotalSuccessfulLeases);
        _outputHelper.WriteLine("TotalFailedLeases " + statistics1.TotalFailedLeases);
        _outputHelper.WriteLine("CurrentAvailablePermits " + statistics1.CurrentAvailablePermits);
        _outputHelper.WriteLine("CurrentQueuedCount " + statistics1.CurrentQueuedCount);

        token1.Cancel();

        await Task.Delay(TimeSpan.FromSeconds(10));
        _outputHelper.WriteLine("------------------------");
        var statistics2 = await rateLimiter.GetStatisticsAsync();

        _outputHelper.WriteLine("TotalSuccessfulLeases " + statistics2.TotalSuccessfulLeases);
        _outputHelper.WriteLine("TotalFailedLeases " + statistics2.TotalFailedLeases);
        _outputHelper.WriteLine("CurrentAvailablePermits " + statistics2.CurrentAvailablePermits);
        _outputHelper.WriteLine("CurrentQueuedCount " + statistics2.CurrentQueuedCount);


        statistics1.TotalSuccessfulLeases.Should().Be(permit);
        statistics1.CurrentQueuedCount.Should().Be(extra);
        statistics1.CurrentAvailablePermits.Should().Be(0);

        statistics2.TotalSuccessfulLeases.Should().Be(permit + extra);
        statistics2.CurrentQueuedCount.Should().Be(0);
        statistics2.CurrentAvailablePermits.Should().Be(permit);
    }

    [Fact]
    public async Task GetFixedWindowRateLimiterTests()
    {
        var permit = 20;
        var summer = new IntTimeSeriesSummer(TimeSpan.FromSeconds(1));
        var rateLimiter = _testApp.Cluster.Client.GetFixedWindowRateLimiter("test");
        await rateLimiter.Configure(new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromSeconds(1),
            PermitLimit = permit,
            AutoReplenishment = true,
            QueueLimit = permit * 2,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });

        var errors = 0;
        var success = 0;
        var calls = 0;

        var sw = Stopwatch.StartNew();
        var tasks = Enumerable.Range(0, 1000).Select(s => Task.Run(async () =>
        {
            Interlocked.Increment(ref calls);
            using (var lease = await rateLimiter.AcquireAsync())
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
        statistics.TotalSuccessfulLeases.Should().Be(success);

        _outputHelper.WriteLine("Samples " + summer.Samples.Count);
        _outputHelper.WriteLine("TotalSuccessfulLeases " + statistics.TotalSuccessfulLeases);
        _outputHelper.WriteLine("TotalFailedLeases " + statistics.TotalFailedLeases);
        _outputHelper.WriteLine("CurrentAvailablePermits " + statistics.CurrentAvailablePermits);
        _outputHelper.WriteLine("CurrentQueuedCount " + statistics.CurrentQueuedCount);
        _outputHelper.WriteLine("Average " + summer.Average());


        foreach (var item in summer.Samples)
            _outputHelper.WriteLine(item.Key.ToString("O") + " " + item.Value);

        summer.Average().Should().Be(permit);
    }

    [Fact]
    public async Task GetSlidingWindowRateLimiterTests()
    {
        var permit = 20;
        var summer = new IntTimeSeriesSummer(TimeSpan.FromSeconds(1));
        var rateLimiter = _testApp.Cluster.Client.GetSlidingWindowRateLimiter("test");
        await rateLimiter.Configure(new SlidingWindowRateLimiterOptions
        {
            Window = TimeSpan.FromSeconds(1),
            PermitLimit = permit,
            AutoReplenishment = true,
            QueueLimit = permit * 2,
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
            using (var lease = await rateLimiter.AcquireAsync())
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
        statistics.TotalSuccessfulLeases.Should().Be(success);

        _outputHelper.WriteLine("Samples " + summer.Samples.Count);
        _outputHelper.WriteLine("TotalSuccessfulLeases " + statistics.TotalSuccessfulLeases);
        _outputHelper.WriteLine("TotalFailedLeases " + statistics.TotalFailedLeases);
        _outputHelper.WriteLine("CurrentAvailablePermits " + statistics.CurrentAvailablePermits);
        _outputHelper.WriteLine("CurrentQueuedCount " + statistics.CurrentQueuedCount);
        _outputHelper.WriteLine("Average " + summer.Average());


        foreach (var item in summer.Samples)
            _outputHelper.WriteLine(item.Key.ToString("O") + " " + item.Value);

        summer.Average().Should().Be(permit);
    }

    [Fact]
    public async Task TokenBucketRateLimiterTests()
    {
        var permit = 10;
        var summer = new IntTimeSeriesSummer(TimeSpan.FromSeconds(1));
        var rateLimiter = _testApp.Cluster.Client.GetTokenBucketRateLimiter("test");
        await rateLimiter.Configure(new TokenBucketRateLimiterOptions
        {
            AutoReplenishment = true,
            QueueLimit = permit * 2,
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
            using (var lease = await rateLimiter.AcquireAsync())
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
        statistics.TotalSuccessfulLeases.Should().Be(success);

        _outputHelper.WriteLine("Samples " + summer.Samples.Count);
        _outputHelper.WriteLine("TotalSuccessfulLeases " + statistics.TotalSuccessfulLeases);
        _outputHelper.WriteLine("TotalFailedLeases " + statistics.TotalFailedLeases);
        _outputHelper.WriteLine("CurrentAvailablePermits " + statistics.CurrentAvailablePermits);
        _outputHelper.WriteLine("CurrentQueuedCount " + statistics.CurrentQueuedCount);
        _outputHelper.WriteLine("Average " + summer.Average());


        foreach (var item in summer.Samples)
            _outputHelper.WriteLine(item.Key.ToString("O") + " " + item.Value);

        summer.Average().Should().Be(permit);
    }
}