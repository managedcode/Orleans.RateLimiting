using ManagedCode.Orleans.RateLimiting.Tests.Cluster;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster.Grains.Interfaces;

namespace ManagedCode.Orleans.RateLimiting.Tests;

[ClassDataSource<TestClusterApplication>(Shared = SharedType.PerTestSession)]
public class ConcurrencyLimiterGrainTests
{
    private readonly TestClusterApplication _testApp;

    public ConcurrencyLimiterGrainTests(TestClusterApplication testApp)
    {
        _testApp = testApp;
    }

    [Test]
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

        (success + errors).ShouldBe(count);
        success.ShouldBe(count);
        errors.ShouldBe(0);
    }

    [Test]
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

        (success + errors).ShouldBe(count);
        success.ShouldBeLessThan(errors);
    }

    [Test]
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

        (success + errors).ShouldBe(count);
        success.ShouldBeLessThan(errors);
    }

    [Test]
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

        (success + errors).ShouldBe(count);
        success.ShouldBeLessThan(errors);
    }
}