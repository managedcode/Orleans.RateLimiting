using FluentAssertions;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster.Grains.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace ManagedCode.Orleans.RateLimiting.Tests;

[Collection(nameof(TestClusterApplication))]
public class ConcurrencyLimiterGrainTests
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly TestClusterApplication _testApp;

    public ConcurrencyLimiterGrainTests(TestClusterApplication testApp, ITestOutputHelper outputHelper)
    {
        _testApp = testApp;
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task GrainIdTests()
    {
        var count = 100;
        var success = 0;
        var errors = 0;

        var tasks = Enumerable.Range(0, count).Select(s => Task.Run(async () =>
        {
            try
            {
                await _testApp.Cluster.Client.GetGrain<ITestConcurrencyLimiterGrain>(s.ToString()).Do();
                Interlocked.Increment(ref success);
            }
            catch
            {
                Interlocked.Increment(ref errors);
            }
        }));

        await Task.WhenAll(tasks);

        (success + errors).Should().Be(count);
        success.Should().Be(count);
        errors.Should().Be(0);
    }

    [Fact]
    public async Task KeyTests()
    {
        var count = 100;
        var success = 0;
        var errors = 0;

        var tasks = Enumerable.Range(0, count).Select(s => Task.Run(async () =>
        {
            try
            {
                await _testApp.Cluster.Client.GetGrain<ITestConcurrencyLimiterGrain>(s.ToString()).Go();
                Interlocked.Increment(ref success);
            }
            catch
            {
                Interlocked.Increment(ref errors);
            }
        }));

        await Task.WhenAll(tasks);

        (success + errors).Should().Be(count);
        success.Should().BeLessThan(errors);
    }

    [Fact]
    public async Task TypeTests()
    {
        var count = 100;
        var success = 0;
        var errors = 0;

        var tasks = Enumerable.Range(0, count).Select(s => Task.Run(async () =>
        {
            try
            {
                await _testApp.Cluster.Client.GetGrain<ITestConcurrencyLimiterGrain>(s.ToString()).Take();
                Interlocked.Increment(ref success);
            }
            catch
            {
                Interlocked.Increment(ref errors);
            }
        }));

        await Task.WhenAll(tasks);

        (success + errors).Should().Be(count);
        success.Should().BeLessThan(errors);
    }

    [Fact]
    public async Task RateLimiterConfigTests()
    {
        var count = 100;
        var success = 0;
        var errors = 0;

        var tasks = Enumerable.Range(0, count).Select(s => Task.Run(async () =>
        {
            try
            {
                await _testApp.Cluster.Client.GetGrain<ITestConcurrencyLimiterGrain>(s.ToString()).Skip();
                Interlocked.Increment(ref success);
            }
            catch
            {
                Interlocked.Increment(ref errors);
            }
        }));

        await Task.WhenAll(tasks);

        (success + errors).Should().Be(count);
        success.Should().BeLessThan(errors);
    }
}