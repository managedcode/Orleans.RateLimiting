using System.Threading.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Client.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Orleans.RateLimiting.Tests.TestApp;

public static class HttpHostProgram
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddSignalR().AddOrleansRateLimiting("SignalR", RateLimitPartitionKind.User);
        services.AddLogging(log => log.AddSimpleConsole());
        services.AddOrleansRateLimiting();
        services.AddOrleansRateLimiterOptions("ip", new FixedWindowRateLimiterOptions
        {
            QueueLimit = 5,
            PermitLimit = 5,
            Window = TimeSpan.FromSeconds(1)
        });

        services.AddOrleansRateLimiterOptions("Anonymous", new FixedWindowRateLimiterOptions
        {
            QueueLimit = 1,
            PermitLimit = 1,
            Window = TimeSpan.FromSeconds(1)
        });

        services.AddOrleansRateLimiterOptions("Authorized", new FixedWindowRateLimiterOptions
        {
            QueueLimit = 2,
            PermitLimit = 2,
            Window = TimeSpan.FromSeconds(1)
        });

        services.AddOrleansRateLimiterOptions("SignalR", new FixedWindowRateLimiterOptions
        {
            QueueLimit = 0,
            PermitLimit = 1,
            Window = TimeSpan.FromSeconds(1)
        });
    }

    public static void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseOrleansIpRateLimiting();
        app.UseOrleansUserRateLimiting();
        app.UseEndpoints(ConfigureEndpoints);
    }

    private static void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllers();
        endpoints.MapHub<TestHub>(nameof(TestHub));
    }
}
