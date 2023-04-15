using ManagedCode.Orleans.RateLimiting.Server.Extensions;
using Orleans.TestingHost;

namespace ManagedCode.Orleans.RateLimiting.Tests.Cluster;

public class TestSiloConfigurations : ISiloConfigurator
{
    public void Configure(ISiloBuilder siloBuilder)
    {
        // add OrleansIdentity
        siloBuilder.AddOrleansIdentity();


        // For test purpose
        // siloBuilder.AddMemoryGrainStorage(OrleansIdentityConstants.SESSION_STORAGE_NAME);

        // siloBuilder.ConfigureServices(services =>
        // {
        //     services.AddSingleton(TestSiloOptions.SessionOption);
        //     // services.AddGrpcOrleansScaling();
        //     //  services.AddApiOrleansScaling();
        // });
    }
}