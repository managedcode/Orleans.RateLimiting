# Architecture

This solution provides Orleans-backed distributed rate limiting for grain calls, ASP.NET Core HTTP pipelines, and SignalR hubs.

## Module Boundaries

```mermaid
flowchart LR
    App["Consumer app"]
    Client["ManagedCode.Orleans.RateLimiting.Client"]
    Core["ManagedCode.Orleans.RateLimiting.Core"]
    Server["ManagedCode.Orleans.RateLimiting.Server"]
    Orchestration["Request orchestration policies"]
    Orleans["Orleans cluster"]
    Runtime["System.Threading.RateLimiting"]

    App --> Client
    App --> Server
    App --> Orchestration
    Orchestration --> Core
    Client --> Core
    Server --> Core
    Client --> Orleans
    Server --> Orleans
    Server --> Runtime
```

## Request Flow

```mermaid
sequenceDiagram
    participant Caller
    participant Middleware as HTTP/SignalR middleware
    participant Client as Orleans client
    participant Grain as Rate limiter grain
    participant Limiter as .NET limiter

    Caller->>Middleware: Request or hub invocation
    Middleware->>Middleware: Resolve limiter attribute and key
    Middleware->>Client: Call limiter grain
    Client->>Grain: Acquire lease
    Grain->>Limiter: Check configured limiter
    Limiter-->>Grain: Lease result
    Grain-->>Middleware: Allowed or rejected
    Middleware-->>Caller: Continue or return 429
```

## Core Contracts

```mermaid
classDiagram
    class IRateLimiterGrain {
        <<interface>>
        AcquireAsync()
        WaitAsync()
    }
    class IRateLimiterGrainWithConfiguration {
        <<interface>>
        GetConfiguration()
        SetConfiguration()
    }
    class IReplenishingRateLimiterGrain {
        <<interface>>
        TryReplenish()
    }
    class IPartitionedRateLimiterGrain {
        <<interface>>
        GetPartition()
    }
    class OrleansRateLimitLease
    class RateLimiterConfig

    IRateLimiterGrain <|-- IRateLimiterGrainWithConfiguration
    IRateLimiterGrain <|-- IReplenishingRateLimiterGrain
    IRateLimiterGrain <|-- IPartitionedRateLimiterGrain
    IRateLimiterGrain --> OrleansRateLimitLease
    IRateLimiterGrainWithConfiguration --> RateLimiterConfig
```

## Design Rules

- Core owns contracts, attributes, models, holder abstractions, and Orleans serialization surrogates.
- Core exposes request orchestration abstractions for user, group, tenant, endpoint, grain, IP, and custom partitions.
- Client owns ASP.NET Core middleware, SignalR filters, and application/client registration helpers.
- Server owns grain implementations, incoming grain call filters, and silo registration helpers.
- Tests prove behaviour through TUnit, Shouldly, TestServer, and Orleans test hosting.
- Public grain interfaces and response semantics are package contracts; treat breaking changes as explicit architecture work.
