using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Client.Middlewares;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Orleans.RateLimiting.Tests.TestApp;

public class HttpHostProgram
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddSignalR();

        builder.Services.AddRateLimiterOptions("ip", new FixedWindowRateLimiterOptions()
        {
            QueueLimit = 5,
            PermitLimit = 10,
            Window = TimeSpan.FromSeconds(1)
        });
       
        builder.Services.AddRateLimiterOptions("Anonymous", new FixedWindowRateLimiterOptions()
        {
            QueueLimit = 1,
            PermitLimit = 1,
            Window = TimeSpan.FromSeconds(1)
        });
        
        builder.Services.AddRateLimiterOptions("Authorized", new FixedWindowRateLimiterOptions()
        {
            QueueLimit = 2,
            PermitLimit = 2,
            Window = TimeSpan.FromSeconds(1)
        });

        
        var app = builder.Build();


        app.MapControllers();
        app.MapHub<TestHub>(nameof(TestHub));

        app.UseMiddleware<RateLimitingMiddleware>();
        
        app.UseRateLimiter();
        
        app.Run();
    }
}