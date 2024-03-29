using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Client.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Orleans.RateLimiting.Tests.TestApp;

public class HttpHostProgram
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddSignalR();
        builder.Services.AddLogging(log=>log.AddSimpleConsole());

        builder.Services.AddOrleansRateLimiting();
        
        builder.Services.AddOrleansRateLimiterOptions("ip", new FixedWindowRateLimiterOptions
        {
            QueueLimit = 5,
            PermitLimit = 5,
            Window = TimeSpan.FromSeconds(1)
        });

        builder.Services.AddOrleansRateLimiterOptions("Anonymous", new FixedWindowRateLimiterOptions
        {
            QueueLimit = 1,
            PermitLimit = 1,
            Window = TimeSpan.FromSeconds(1)
        });

        builder.Services.AddOrleansRateLimiterOptions("Authorized", new FixedWindowRateLimiterOptions
        {
            QueueLimit = 2,
            PermitLimit = 2,
            Window = TimeSpan.FromSeconds(1)
        });


        var app = builder.Build();


        app.MapControllers();
        app.MapHub<TestHub>(nameof(TestHub));

        app.UseOrleansIpRateLimiting();
        app.UseOrleansUserRateLimiting();

        //app.UseRateLimiter();

        app.Run();
    }
}