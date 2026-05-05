using ManagedCode.Orleans.RateLimiting.Tests.Cluster;

namespace ManagedCode.Orleans.RateLimiting.Tests;

[ClassDataSource<TestClusterApplication>(Shared = SharedType.PerTestSession)]
public class WebApiTests
{
    private readonly TestClusterApplication _testApp;

    public WebApiTests(TestClusterApplication testApp)
    {
        _testApp = testApp;
    }


    [Test]
    public async Task ControllerTest()
    {
        var client = _testApp.CreateClient();

        var count = 25;
        var success = 0;
        var errors = 0;

        var tasks = Enumerable.Range(0, count).Select(s => Task.Run(async () =>
        {
            try
            {
                var response = await client.GetAsync("/test/authorize");
                var content = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                    Interlocked.Increment(ref success);
                else
                    Interlocked.Increment(ref errors);
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