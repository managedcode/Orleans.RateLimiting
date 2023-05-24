using FluentAssertions;
using ManagedCode.Orleans.RateLimiting.Tests.Cluster;
using ManagedCode.Orleans.RateLimiting.Tests.TestApp;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;
using Xunit.Abstractions;

namespace ManagedCode.Orleans.RateLimiting.Tests;

[Collection(nameof(TestClusterApplication))]
public class SignalRTests
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly TestClusterApplication _testApp;

    public SignalRTests(TestClusterApplication testApp, ITestOutputHelper outputHelper)
    {
        _testApp = testApp;
        _outputHelper = outputHelper;
    }


    [Fact]
    public async Task Some()
    {
        try
        {
            var anonymousHub11 = _testApp.CreateSignalRClient(nameof(TestHub));
            await anonymousHub11.StartAsync();
            anonymousHub11.State.Should().Be(HubConnectionState.Connected);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        
        var anonymousHub = _testApp.CreateSignalRClient(nameof(TestHub));
        await anonymousHub.StartAsync();
        anonymousHub.State.Should().Be(HubConnectionState.Connected);

    }
}