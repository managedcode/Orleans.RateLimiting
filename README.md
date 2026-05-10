# ManagedCode.Orleans.RateLimiting

Distributed rate limiting for Microsoft Orleans applications.

The library wraps `System.Threading.RateLimiting` behind Orleans grains so the same limiter can be shared across silos, HTTP middleware, SignalR hubs, and grain calls.

## Packages

- `ManagedCode.Orleans.RateLimiting.Core` — shared grain contracts, attributes, options, leases, and Orleans serializers.
- `ManagedCode.Orleans.RateLimiting.Server` — Orleans grain implementations, incoming grain call filters, and silo registration helpers.
- `ManagedCode.Orleans.RateLimiting.Client` — Orleans client, ASP.NET Core middleware, SignalR, and HTTP attribute integration.

## Features

- Fixed window, sliding window, token bucket, and concurrency limiters.
- Distributed limiter state through Orleans grains.
- Durable quota snapshots through Orleans grain storage, with reset and state deletion support.
- Grain method attributes for grain-call rate limiting.
- Request orchestration for per-user, per-group, per-tenant, per-role, per-IP, per-endpoint, per-grain, and custom partitions.
- ASP.NET Core request middleware plus controller attributes for IP, anonymous user, authorized user, and role-aware limiting.
- SignalR hub invocation filtering through the same request orchestration pipeline.
- Central package management and .NET 10 build/test/coverage workflow.

## Requirements

- .NET SDK 10
- Microsoft Orleans 10

## Installation

Install the server package in the silo host:

```sh
dotnet add package ManagedCode.Orleans.RateLimiting.Server
```

Install the client package in applications that call limiters or use ASP.NET Core middleware:

```sh
dotnet add package ManagedCode.Orleans.RateLimiting.Client
```

## Silo Setup

Register the Orleans rate-limiting services, configure the package storage provider, and add any limiter defaults that should be enforced by grain call filters.

```csharp
using ManagedCode.Orleans.RateLimiting.Server;
using ManagedCode.Orleans.RateLimiting.Server.Extensions;
using ManagedCode.Orleans.RateLimiting.Server.Options;

siloBuilder.AddAzureTableGrainStorage(
    RateLimiterStorageNames.StorageProviderName,
    options =>
    {
        options.ConfigureTableServiceClient(connectionString);
        options.DeleteStateOnClear = true;
    });

siloBuilder.AddOrleansRateLimiting();
siloBuilder.Services.Configure<RateLimiterPersistenceOptions>(options =>
{
    options.StateFlushPeriod = TimeSpan.FromMinutes(5);
});

siloBuilder.AddOrleansConcurrencyLimiter(options =>
{
    options.PermitLimit = 10;
    options.QueueLimit = 15;
});

siloBuilder.AddOrleansFixedWindowRateLimiter(options =>
{
    options.PermitLimit = 10;
    options.QueueLimit = 15;
    options.Window = TimeSpan.FromSeconds(1);
});

siloBuilder.AddOrleansSlidingWindowRateLimiter(options =>
{
    options.PermitLimit = 10;
    options.QueueLimit = 15;
    options.Window = TimeSpan.FromSeconds(1);
    options.SegmentsPerWindow = 2;
});

siloBuilder.AddOrleansTokenBucketRateLimiter(options =>
{
    options.TokenLimit = 10;
    options.QueueLimit = 15;
    options.TokensPerPeriod = 2;
    options.ReplenishmentPeriod = TimeSpan.FromSeconds(1);
});
```

## Durable State Storage

Rate limiter grains persist their configuration and quota snapshot through Orleans grain storage. Register the storage provider before `AddOrleansRateLimiting()` and always use `RateLimiterStorageNames.StorageProviderName` from the `ManagedCode.Orleans.RateLimiting.Server` namespace:

```csharp
siloBuilder.AddAzureTableGrainStorage(
    RateLimiterStorageNames.StorageProviderName,
    options =>
    {
        options.ConfigureTableServiceClient(connectionString);
        options.DeleteStateOnClear = true;
    });
```

For local development or tests, use the same provider name with an in-memory provider:

```csharp
siloBuilder.AddMemoryGrainStorage(RateLimiterStorageNames.StorageProviderName);
```

`RateLimiterPersistenceDefaults.StorageProviderName` remains available as a compatibility alias, but new host setup should use `RateLimiterStorageNames.StorageProviderName`.

Limiter grains update in-memory state on acquire/release and flush changed state to Orleans storage every five minutes by default. They also force a write during configuration, reset, and activation deactivation. Configure the flush interval when tests or workloads need a different value:

```csharp
siloBuilder.Services.Configure<RateLimiterPersistenceOptions>(options =>
{
    options.StateFlushPeriod = TimeSpan.FromSeconds(30);
});
```

State cleanup has two different operations:

```csharp
var limiter = clusterClient.GetFixedWindowRateLimiter("tenant:api");

await limiter.ResetAsync();
await limiter.DeleteStateAsync();
```

`ResetAsync()` clears the current quota window or active concurrency leases while keeping the limiter configuration in storage. Use it for admin reset flows where the limiter still exists.

`DeleteStateAsync()` clears the Orleans persistent state record through `IPersistentState.ClearStateAsync()`. It removes the stored limiter configuration, quota snapshot, counters, and active lease state, then returns the in-memory activation to the silo defaults. Use it when the limiter key is no longer needed. Physical deletion is provider-defined; Azure Table and Blob storage delete the row/blob when their `DeleteStateOnClear` option is enabled.

## Direct Limiter Usage

Limiters are available as extensions on `IGrainFactory` and `IClusterClient`.

```csharp
var limiter = clusterClient.GetConcurrencyLimiter("tenant:user");

await limiter.Configure(new ConcurrencyLimiterOptions
{
    PermitLimit = 20,
    QueueLimit = 40,
    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
});

await using var lease = await limiter.AcquireAsync();
if (!lease.IsAcquired)
{
    Console.WriteLine(lease.Reason);
    Console.WriteLine(lease.RetryAfter);
    return;
}

// Continue protected work.
```

You can also create option-backed holders when configuration should be checked before acquisition:

```csharp
var limiter = clusterClient.GetFixedWindowRateLimiter(
    "tenant:api",
    new FixedWindowRateLimiterOptions
    {
        PermitLimit = 100,
        QueueLimit = 0,
        Window = TimeSpan.FromMinutes(1)
    });

await using var lease = await limiter.AcquireAndConfigureAsync();
lease.ThrowIfNotAcquired();
```

Reset the durable quota state through the limiter holder when an administrative workflow needs to clear the current limiter window or active concurrency leases:

```csharp
await limiter.ResetAsync();
```

Delete the durable state when the limiter key should be removed completely and future calls should start from silo defaults:

```csharp
await limiter.DeleteStateAsync();
```

## Request Orchestration

Request orchestration maps one logical request into one or more distributed limiter partitions. This is the preferred API for new applications because it lets a cluster enforce combined limits such as per user, per group, per tenant, per endpoint, and custom metadata keys.

```csharp
builder.Services.AddOrleansRateLimiting(options =>
{
    options.AddUser("user-checkout", required: true);
    options.AddGroup("group-checkout");
    options.AddTenant("tenant-checkout");
    options.AddCustom("route-checkout", metadataKey: "route");
});
```

Rules without a policy name apply to the default request pipeline. Use named policies when different surfaces need independent limits.

```csharp
builder.Services.AddOrleansRateLimiting(options =>
{
    options.AddToPolicy("checkout-api", RateLimitPartitionKind.User, "user-checkout", required: true);
    options.AddToPolicy("checkout-api", RateLimitPartitionKind.Group, "group-checkout");
});

app.UseOrleansRequestRateLimiting("checkout-api");
```

The default `IRateLimitRequestOrchestrator` uses registered `IRateLimitRequestPolicy` instances, `IRateLimitRequestKeyResolver`, named `RateLimiterConfig` values, and Orleans grains to build a `GroupLimiterHolder`.

```csharp
var request = new RateLimitRequestContext
{
    OperationName = "checkout",
    PolicyName = "checkout-api",
    UserId = "user-123",
    GroupId = "group-a",
    TenantId = "tenant-main",
    Metadata = new Dictionary<string, string>
    {
        ["route"] = "/checkout"
    }
};

await using var group = await orchestrator.CreateLimiterGroupAsync(request);
var rejectedLease = await group.AcquireAsync();
if (rejectedLease is not null)
{
    throw rejectedLease.ToException();
}
```

Implement custom `IRateLimitRequestPolicy` or `IRateLimitRequestKeyResolver` when the default context fields are not enough.

## Grain Attributes

Decorate grain methods to enforce rate limiting through incoming grain call filters.

```csharp
public class TestFixedWindowRateLimiterGrain : Grain, ITestFixedWindowRateLimiterGrain
{
    [FixedWindowRateLimiter]
    public Task<string> ByGrainId()
    {
        return Task.FromResult("ok");
    }

    [FixedWindowRateLimiter(KeyType.Key, "shared-key")]
    public Task<string> BySharedKey()
    {
        return Task.FromResult("ok");
    }

    [FixedWindowRateLimiter(KeyType.GrainType, permitLimit: 2, queueLimit: 1)]
    public Task<string> ByGrainType()
    {
        return Task.FromResult("ok");
    }
}
```

## ASP.NET Core Usage

Register named limiter options and configure request orchestration rules.

```csharp
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

builder.Services.AddOrleansRateLimiting(options =>
{
    options.AddIpAddress("ip");
    options.AddUser("Authorized");
});
```

```csharp
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseOrleansRequestRateLimiting();

// Attribute-based middleware is still available for existing controllers.
app.UseOrleansIpRateLimiting();
app.UseOrleansUserRateLimiting();

app.MapControllers();
```

Apply HTTP limiter attributes to controllers or actions.

```csharp
[AuthorizedIpRateLimiter("Authorized")]
[AnonymousIpRateLimiter("Anonymous")]
[InRoleIpRateLimiter("Authorized", "Admin")]
[HttpGet("limited")]
public Task<ActionResult<string>> GetLimited()
{
    return Task.FromResult<ActionResult<string>>("OK");
}
```

## SignalR Usage

SignalR hub methods can be rate limited through the same request orchestration infrastructure.

```csharp
builder.Services.AddOrleansRateLimiterOptions("SignalR", new FixedWindowRateLimiterOptions
{
    PermitLimit = 10,
    QueueLimit = 0,
    Window = TimeSpan.FromSeconds(1)
});

builder.Services
    .AddSignalR()
    .AddOrleansRateLimiting("SignalR", RateLimitPartitionKind.User);
```

The built-in hub filter uses a named SignalR policy by default, so hub rules do not bleed into HTTP request middleware rules. It builds a `RateLimitRequestContext` from hub method name, user identifier, claims, IP address, and hub resource name.

## Development

This repository targets .NET 10 with central package management.

```sh
dotnet restore ManagedCode.Orleans.RateLimiting.sln
dotnet build ManagedCode.Orleans.RateLimiting.sln --configuration Release --no-restore
dotnet test --solution ManagedCode.Orleans.RateLimiting.sln --configuration Release --no-build --verbosity normal
dotnet format ManagedCode.Orleans.RateLimiting.sln --verify-no-changes
```

Coverage uses local tools from `.config/dotnet-tools.json`.

```sh
dotnet tool restore
dotnet tool run coverlet ManagedCode.Orleans.RateLimiting.Tests/bin/Release/net10.0/ManagedCode.Orleans.RateLimiting.Tests.dll --target "dotnet" --targetargs "test --project ManagedCode.Orleans.RateLimiting.Tests/ManagedCode.Orleans.RateLimiting.Tests.csproj --configuration Release --no-build --no-restore" --format cobertura --output artifacts/coverage/coverage.cobertura.xml --exclude "[ManagedCode.Orleans.RateLimiting.Tests]*" --threshold 85 --threshold-type line --threshold-stat total
dotnet tool run reportgenerator -reports:"artifacts/coverage/coverage.cobertura.xml" -targetdir:"artifacts/coverage-report" -reporttypes:"HtmlSummary;MarkdownSummaryGithub"
```

Current local coverage after the .NET 10 migration and request-orchestration refactor is above the 85% line-coverage target.

## Contributing

Issues and pull requests are welcome in the [GitHub repository](https://github.com/managedcode/Orleans.RateLimiting).
