using FluentAssertions;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;
using Xunit;
using Xunit.Abstractions;

namespace ManagedCode.Orleans.RateLimiting.Tests;

[Collection(nameof(TestClusterApplication))]
public class WebApiTests
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly TestClusterApplication _testApp;

    public WebApiTests(TestClusterApplication testApp, ITestOutputHelper outputHelper)
    {
        _testApp = testApp;
        _outputHelper = outputHelper;
    }


    [Fact]
    public async Task ControllerTest()
    {
        var client = _testApp.CreateClient();
        
        int count = 25;
        int success = 0;
        int errors = 0;
        
        var tasks = Enumerable.Range(0, count).Select(s => Task.Run(async () =>
        {
            try
            {
                var response = await client.GetAsync("/test/authorize");
                var content = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    Interlocked.Increment(ref success);                    
                }
                else
                {
                    Interlocked.Increment(ref errors);
                }

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