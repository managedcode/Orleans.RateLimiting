using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Orleans.RateLimiting.Core.Models.Holders;

public class GroupLimiterHolder :  IAsyncDisposable, IDisposable
{
    private Dictionary<ILimiterHolder, OrleansRateLimitLease?> _holders = new();
   public bool AddLimiter(ILimiterHolder? holder)
   {
       if (holder is not null)
       {
           _holders.Add(holder, null);
           return true;
       }

       return false;

   }
   
   public async Task<OrleansRateLimitLease?> AcquireAsync()
   {
       foreach (var holder in _holders.Keys)
       {
           var lease = await holder.AcquireAndConfigureAsync();
           if (lease.IsAcquired)
           {
               _holders[holder] = lease;   
           }
           else
           {
               return lease;
           }
       }

       return null;
   }

   public async ValueTask DisposeAsync()
   {
       await Task.WhenAll(_holders.Values
           .Where(w => w != null)
           .Select(s => s.DisposeAsync().AsTask()));
   }

   public void Dispose()
   {
       foreach (var pair in _holders)
       {
           pair.Value?.Dispose();
       }
   }
}