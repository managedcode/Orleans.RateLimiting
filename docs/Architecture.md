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
    participant Orchestrator as Request orchestrator
    participant Client as Orleans client
    participant Grain as Rate limiter grain
    participant Limiter as .NET limiter

    Caller->>Middleware: Request or hub invocation
    Middleware->>Orchestrator: Build request context
    Orchestrator->>Orchestrator: Resolve rules and partition keys
    Orchestrator->>Client: Build limiter holder group
    Client->>Grain: Acquire lease
    Grain->>Limiter: Check configured limiter
    Limiter-->>Grain: Lease result
    Grain-->>Orchestrator: Allowed or rejected
    Orchestrator-->>Middleware: Group acquisition result
    Middleware-->>Caller: Continue or return 429
```

## Core Contracts

```mermaid
classDiagram
    class IRateLimiterGrain {
        <<interface>>
        AcquireAsync()
        GetStatisticsAsync()
        ReleaseLease()
    }
    class IRateLimiterGrainWithConfiguration {
        <<interface>>
        GetConfiguration()
        ConfigureAsync()
        AcquireAndCheckConfigurationAsync()
    }
    class IRateLimitRequestOrchestrator {
        <<interface>>
        CreateLimiterGroupAsync()
    }
    class IRateLimitRequestPolicy {
        <<interface>>
        GetRulesAsync()
    }
    class IRateLimitRequestKeyResolver {
        <<interface>>
        ResolveKeyAsync()
    }
    class OrleansRateLimitLease
    class RateLimiterConfig
    class RateLimitRequestRule
    class RateLimitRequestContext
    class GroupLimiterHolder

    IRateLimiterGrain <|-- IRateLimiterGrainWithConfiguration
    IRateLimiterGrain --> OrleansRateLimitLease
    IRateLimiterGrainWithConfiguration --> RateLimiterConfig
    IRateLimitRequestOrchestrator --> IRateLimitRequestPolicy
    IRateLimitRequestOrchestrator --> IRateLimitRequestKeyResolver
    IRateLimitRequestPolicy --> RateLimitRequestRule
    IRateLimitRequestOrchestrator --> RateLimitRequestContext
    IRateLimitRequestOrchestrator --> GroupLimiterHolder
```

## Design Rules

- Core owns contracts, attributes, models, holder abstractions, and Orleans serialization surrogates.
- Core exposes request orchestration abstractions and default implementations for user, group, tenant, role, endpoint, grain, IP, and custom partitions.
- Client owns ASP.NET Core middleware, SignalR filters, and application/client registration helpers.
- Server owns grain implementations, incoming grain call filters, and silo registration helpers.
- Tests prove behaviour through TUnit, Shouldly, TestServer, and Orleans test hosting.
- Public grain interfaces, orchestration rules, and lease semantics are package contracts; treat breaking changes as explicit architecture work.

## Current Architecture Decisions

- New application-level limits should use `IRateLimitRequestOrchestrator` and `UseOrleansRequestRateLimiting`.
- HTTP and SignalR integrations should use named policies when both run in the same application.
- Attribute-based HTTP middleware remains available for existing controller flows, but it is no longer the only client path.
- SignalR rate limiting is policy-driven and registered through `AddSignalR().AddOrleansRateLimiting(...)`.
- Removed commented partitioned/replenishing grain placeholders. Real partitioning now lives at the request orchestration layer, which maps partitions to existing Orleans-backed limiter grains.
