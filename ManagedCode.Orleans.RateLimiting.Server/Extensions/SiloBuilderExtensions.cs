using Orleans.Hosting;

namespace ManagedCode.Orleans.RateLimiting.Server.Extensions;

public static class SiloBuilderExtensions
{
    /// <summary>
    ///     Add incoming grain filter for authorization
    /// </summary>
    /// <param name="siloBuilder"></param>
    /// <returns></returns>
    public static ISiloBuilder AddOrleansIdentity(this ISiloBuilder siloBuilder)
    {
        //siloBuilder.AddIncomingGrainCallFilter<GrainAuthorizationIncomingFilter>();
        return siloBuilder;
    }
}