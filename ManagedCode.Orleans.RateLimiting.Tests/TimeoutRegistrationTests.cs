using ManagedCode.Orleans.RateLimiting.Client.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Services;
using ManagedCode.Orleans.RateLimiting.Server.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ManagedCode.Orleans.RateLimiting.Tests;

public class TimeoutRegistrationTests
{
    private const int OneFilter = 1;
    private const string SiloFilterName = "SiloRateLimiterTimeoutFilter";

    [Test]
    public void ServiceCollectionRegistrationInstallsOneDeadlineFilter()
    {
        var services = new ServiceCollection();
        services.AddOrleansRateLimiting();
        services.AddOrleansRateLimiting();
        using var provider = services.BuildServiceProvider();
        provider.GetServices<IOutgoingGrainCallFilter>().OfType<RateLimiterTimeoutFilter>().Count().ShouldBe(OneFilter);
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public void CoHostedRegistrationUsesOneSiloDeadlineRegardlessOfOrder(bool clientFirst)
    {
        using var host = new HostBuilder().UseOrleans(silo =>
        {
            if (clientFirst) silo.Services.AddOrleansRateLimiting();
            silo.AddOrleansRateLimiting();
            silo.AddOrleansRateLimiting();
            if (!clientFirst) silo.Services.AddOrleansRateLimiting();
        }).Build();
        var filters = host.Services.GetServices<IOutgoingGrainCallFilter>().OfType<RateLimiterTimeoutFilter>().ToArray();
        filters.Length.ShouldBe(OneFilter);
        filters.Single().GetType().Name.ShouldBe(SiloFilterName);
    }
}
