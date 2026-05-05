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
    public async Task HubConnectionsStart()
    {
        var firstConnection = _testApp.CreateSignalRClient(nameof(TestHub));
        await firstConnection.StartAsync();
        firstConnection.State.ShouldBe(HubConnectionState.Connected);

        var secondConnection = _testApp.CreateSignalRClient(nameof(TestHub));
        await secondConnection.StartAsync();
        secondConnection.State.ShouldBe(HubConnectionState.Connected);
    }

    [Test]
    public async Task HubInvocationsAreRateLimited()
    {
        var connections = Enumerable.Range(0, 4).Select(_ => _testApp.CreateSignalRClient(nameof(TestHub))).ToArray();
        try
        {
            foreach (var connection in connections)
                await connection.StartAsync();

            var calls = connections.Select(InvokeHubAsync);
            var results = await Task.WhenAll(calls);

            results.Count(result => result).ShouldBe(1);
            results.Count(result => !result).ShouldBe(3);
        }
        finally
        {
            foreach (var connection in connections)
                await connection.DisposeAsync();
        }
    }

    private static async Task<bool> InvokeHubAsync(HubConnection connection)
    {
        try
        {
            await connection.InvokeAsync<int>(nameof(TestHub.DoTest));
            return true;
        }
        catch
        {
            return false;
        }
    }
}
