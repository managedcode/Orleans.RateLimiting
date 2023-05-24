using ManagedCode.Orleans.RateLimiting.Client.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Orleans.RateLimiting.Tests.TestApp.Controllers;

[Route("test")]
[IpRateLimiter("ip")]
public class TestController : ControllerBase
{
    [AuthorizedIpRateLimiter("Authorized")]
    [AnonymousIpRateLimiter("Authorized")]
    [InRoleIpRateLimiter("Authorized", "Admin")]
    [HttpGet("authorize")]
    public async Task<ActionResult<string>> Authorize()
    {
        await Task.Delay(500);
        return "Authorize";
    }
}