# Orleans.RateLimiting

Orleans.RateLimiting is a library for Microsoft Orleans that provides a set of rate limiting algorithms for controlling
the flow of requests in your distributed applications.
It is designed to be easy to use and to integrate with your Orleans-based applications seamlessly.
With Orleans.RateLimiting, you can ensure your applications handle a safe number of requests without the risk of
overloading your system resources.
RateLimiting
on [learn.microsoft.com](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-7.0)
and [devblogs.microsoft.com](https://devblogs.microsoft.com/dotnet/announcing-rate-limiting-for-dotnet/)

## Features

- Supports 4 types of rate limiting algorithms:
    - Fixed Window Rate Limiter
    - Concurrency Limiter
    - Sliding Window Rate Limiter
    - Token Bucket Rate Limiter
- Easy integration with Microsoft Orleans
- Configurable rate limiting options
- Comprehensive documentation and examples

## Installation

You can install Orleans.RateLimiting via NuGet Package Manager:

```sh
// for Client
Install-Package ManagedCode.Orleans.RateLimiting.Client
```

```sh
// for Server
Install-Package ManagedCode.Orleans.RateLimiting.Server
```

then add the following to your `SiloHostBuilder` or `ClientBuilder`:

```csharp
// for Client
clientBuilder.AddOrleansRateLimiting();

// for Server
siloBuilder.AddOrleansRateLimiting();
```

Also if you would like to use incoming filter and **Attributes**, you have to add default options for Limiter:

```csharp
//Add default options and IncomingFilter
siloBuilder.AddOrleansConcurrencyLimiter(options =>
{
    options.PermitLimit = 10;
    options.QueueLimit = 15;
});

//Add default options and IncomingFilter
siloBuilder.AddOrleansFixedWindowRateLimiter(options =>
{
    options.PermitLimit = 10;
    options.QueueLimit = 15;
    options.Window = TimeSpan.FromSeconds(1);
});

//Add default options and IncomingFilter
siloBuilder.AddOrleansSlidingWindowRateLimiter(options =>
{
    options.PermitLimit = 10;
    options.QueueLimit = 15;
    options.Window = TimeSpan.FromSeconds(1);
    options.SegmentsPerWindow = 2;

});

//Add default options and IncomingFilter
siloBuilder.AddOrleansTokenBucketRateLimiter(options =>
{
    options.TokenLimit = 10;
    options.QueueLimit = 15;
    options.TokensPerPeriod = 2;
    options.ReplenishmentPeriod = TimeSpan.FromSeconds(1);
});
```

## Usage

To use Orleans.RateLimiting in your application, first configure the desired rate limiter:

```csharp
var rateLimiter = _client.GetConcurrencyLimiter("test");
await rateLimiter.Configure(new ConcurrencyLimiterOptions
{
    PermitLimit = permit,
    QueueLimit = permit * 2,
    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
});
```

Then, acquire a lease before making a request:

```csharp
await using var lease = await rateLimiter.AcquireAsync();
if (lease.IsAcquired)
{
    // do something
}
else
{
    Console.WriteLine(lease.Reason); // reason why the lease was not acquired
    Console.WriteLine(lease.RetryAfter); //TimeSpan to wait before retrying
}
```

The following rate limiters are provided as extensions for `IGrainFactory` and `IClusterClient`

- Fixed Window Rate Limiter

```csharp
var fixedWindowRateLimiter = _factory.GetFixedWindowRateLimiter("key");
```

- Concurrency Limiter

```csharp
var concurrencyLimiter = _factory.GetConcurrencyLimiter("key");
```

- Sliding Window Rate Limiter

```csharp
var slidingWindowRateLimiter = _factory.GetSlidingWindowRateLimiter("key");
```

- Token Bucket Rate Limiter

```csharp
var tokenBucketRateLimiter = _factory.GetTokenBucketRateLimiter("key");
```

### Attrubutes for Grains

You can use attributes to decorate your grain methods and apply rate limiting to them.
Make sure you check configuration section for default options.

```csharp
public class TestFixedWindowRateLimiterGrain : Grain, ITestFixedWindowRateLimiterGrain
{
    [FixedWindowRateLimiter] //GrainId as key, default options
    public async Task<string> Do()
    {
        await Task.Delay(TimeSpan.FromSeconds(5));
        return "Do";
    }

    [FixedWindowRateLimiter(KeyType.Key, "go")] //String as Key, default options
    public async Task<string> Go()
    {
        await Task.Delay(TimeSpan.FromSeconds(5));
        return "Go";
    }

    [FixedWindowRateLimiter(KeyType.GrainType, permitLimit:2, queueLimit:1)] //GrainType as Key, custom options, some of them are default (check Attribute)
    public async Task<string> Take()
    {
        await Task.Delay(TimeSpan.FromSeconds(5));
        return "Take";
    }
}
```

### Attrubutes for WebAPI

You can define OrleansRateLimiterOptions with specific name.
``` cs
builder.Services.AddOrleansRateLimiterOptions("ip", new FixedWindowRateLimiterOptions
{
    QueueLimit = 5,
    PermitLimit = 10,
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
        
```

then add middelware
``` cs
app.UseOrleansIpRateLimiting(); // as earlier as possible
.....
app.UseRouting();
app.UseCors();
app.MapControllers();

//Authentication should always be placed before Authorization.
app.UseAuthentication();
app.UseAuthorization();
app.UseOrleansUserRateLimiting(); // after Authorization and Authorization
.....
```

Finally you can add attributes to controller or single methods:
``` cs
[AuthorizedIpRateLimiter("Authorized")]
[AnonymousIpRateLimiter("Authorized")]
[InRoleIpRateLimiter("Authorized", "Admin")]
[HttpGet("get_some")]
public async Task<ActionResult<string>> GetSome()
{
    await Task.Delay(300);
    return "OK";
}
```
## Contributing

We welcome contributions to Orleans.RateLimiting!
Feel free to submit issues, feature requests, and pull requests on
the [GitHub repository](https://github.com/yourusername/Orleans.RateLimiter).
