using System.Diagnostics;
using System.Threading.RateLimiting;
using FluentAssertions;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster.Grains.Interfaces;
using ManagedCode.TimeSeries.Summers;
using Xunit;
using Xunit.Abstractions;

namespace ManagedCode.Orleans.RateLimiting.Tests;

[Collection(nameof(TestClusterApplication))]
public class GrainsRateLimiterTests
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly TestClusterApplication _testApp;

    public GrainsRateLimiterTests(TestClusterApplication testApp, ITestOutputHelper outputHelper)
    {
        _testApp = testApp;
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task GetConcurrencyLimiterGrainIdTests()
    {
        int count = 100;
        int success = 0;
        int errors = 0;
        
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
    public async Task GetConcurrencyLimiterKeyTests()
    {
        int count = 100;
        int success = 0;
        int errors = 0;
        
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
}