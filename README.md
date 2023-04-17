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
Install-Package anagedCode.Orleans.RateLimiting.Client
```

```sh
// for Server
Install-Package anagedCode.Orleans.RateLimiting.Server
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

## Contributing

We welcome contributions to Orleans.RateLimiting!
Feel free to submit issues, feature requests, and pull requests on
the [GitHub repository](https://github.com/yourusername/Orleans.RateLimiter).
