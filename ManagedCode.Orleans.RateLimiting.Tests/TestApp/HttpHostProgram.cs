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

       

        
        var app = builder.Build();


        app.MapControllers();
        app.MapHub<TestHub>(nameof(TestHub));

        app.UseMiddleware<RateLimitingMiddleware>();
        
        app.Run();
    }
}