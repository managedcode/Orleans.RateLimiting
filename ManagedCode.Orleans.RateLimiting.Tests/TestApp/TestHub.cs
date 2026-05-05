using Microsoft.AspNetCore.SignalR;

namespace ManagedCode.Orleans.RateLimiting.Tests.TestApp;

public class TestHub : Hub
{
    public async Task<int> DoTest()
    {
        _ = Context.ConnectionId;
        await Task.Delay(250);
        return Random.Shared.Next();
    }
}
