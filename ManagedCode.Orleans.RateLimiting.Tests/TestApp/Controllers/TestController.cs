using ManagedCode.Orleans.RateLimiting.Client.Attributes;
using ManagedCode.Orleans.RateLimiting.Core.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Orleans.RateLimiting.Tests.TestApp.Controllers;

[Route("test")]
[AnonymousIpRateLimiter("ipsdfsdf")]
public class TestController : ControllerBase
{
    [AuthorizedIpRateLimiter("ipsdfsdf")]
    [HttpGet("authorize")]
    public ActionResult<string> Authorize()
    {
        return "Authorize";
    }
    
    [ConcurrencyLimiter("LimitByUser")]
    [HttpGet("anonymous")]
    public ActionResult<string> Anonymous()
    {
        return "Anonymous";
    }
    
    [HttpGet("admin")]
    public ActionResult<string> Admin()
    {
        return "admin";
    }
    
    [HttpGet("moderator")]
    public ActionResult<string> Moderator()
    {
        return "moderator";
    }
    
    [HttpGet("common")]
    public ActionResult<string> Common()
    {
        return "common";
    }
}