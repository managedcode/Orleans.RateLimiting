using ManagedCode.Orleans.RateLimiting.Client.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Services;
using ManagedCode.Orleans.RateLimiting.Server.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ManagedCode.Orleans.RateLimiting.Tests;

public class TimeoutRegistrationTests
{
    private const int OneProvider = 1;
    private const string SiloProviderName = "SiloRateLimiterTimeoutProvider";

    [Test]
    public void ServiceCollectionRegistrationInstallsOneDeadlineProviderWithoutFilters()
    {
        var services = new ServiceCollection();
        services.AddOrleansRateLimiting();
        services.AddOrleansRateLimiting();
        using var provider = services.BuildServiceProvider();
        provider.GetServices<RateLimiterTimeoutProvider>().Count().ShouldBe(OneProvider);
        provider.GetServices<IOutgoingGrainCallFilter>().ShouldBeEmpty();
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
        var providers = host.Services.GetServices<RateLimiterTimeoutProvider>().ToArray();
        providers.Length.ShouldBe(OneProvider);
        providers.Single().GetType().Name.ShouldBe(SiloProviderName);
        host.Services.GetServices<IOutgoingGrainCallFilter>().ShouldBeEmpty();
    }
}
