using ManagedCode.Orleans.RateLimiting.Tests.Cluster;
using ManagedCode.Orleans.RateLimiting.Tests.TestApp;
using Microsoft.AspNetCore.SignalR.Client;

namespace ManagedCode.Orleans.RateLimiting.Tests;

[ClassDataSource<TestClusterApplication>(Shared = SharedType.PerTestSession)]
public class SignalRTests
{
    private readonly TestClusterApplication _testApp;

    public SignalRTests(TestClusterApplication testApp)
    {
        _testApp = testApp;
    }


    [Test]
    public async Task Some()
    {
        try
        {
            var anonymousHub11 = _testApp.CreateSignalRClient(nameof(TestHub));
            await anonymousHub11.StartAsync();
            anonymousHub11.State.ShouldBe(HubConnectionState.Connected);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        var anonymousHub = _testApp.CreateSignalRClient(nameof(TestHub));
        await anonymousHub.StartAsync();
        anonymousHub.State.ShouldBe(HubConnectionState.Connected);

    }
}