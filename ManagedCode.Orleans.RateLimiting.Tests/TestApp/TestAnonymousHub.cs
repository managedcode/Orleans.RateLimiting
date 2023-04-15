using Microsoft.AspNetCore.SignalR;

namespace ManagedCode.Orleans.RateLimiting.Tests.TestApp;

public class TestAnonymousHub : Hub
{
    public Task<int> DoTest()
    {
        return Task.FromResult(new Random().Next());
    }
}