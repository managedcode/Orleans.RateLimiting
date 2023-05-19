using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace ManagedCode.Orleans.RateLimiting.Core.Models.Holders;

public interface ILimiterHolder
{
    Task<OrleansRateLimitLease> AcquireAsync(int permitCount = 1);
    Task<OrleansRateLimitLease> AcquireAndConfigureAsync(int permitCount = 1);
    ValueTask<RateLimiterStatistics?> GetStatisticsAsync();
}