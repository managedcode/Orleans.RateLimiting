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
    public async Task Some()
    {
        var client = _testApp.CreateClient();
        var response = await client.GetAsync("/test/authorize");
        response.EnsureSuccessStatusCode();
    }
}