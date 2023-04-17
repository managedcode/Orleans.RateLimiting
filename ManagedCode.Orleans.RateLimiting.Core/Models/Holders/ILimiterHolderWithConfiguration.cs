using System.Threading.Tasks;

namespace ManagedCode.Orleans.RateLimiting.Core.Models.Holders;

public interface ILimiterHolderWithConfiguration<in TOptions> : ILimiterHolder
{
    Task<OrleansRateLimitLease> AcquireAndCheckConfigurationAsync(TOptions options);
    Task<OrleansRateLimitLease> AcquireAndCheckConfigurationAsync(int permitCount, TOptions options);
}