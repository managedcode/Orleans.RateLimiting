# AGENTS.md

Project: ManagedCode.Orleans.RateLimiting.Core

## Purpose

Defines shared Orleans rate-limiter contracts, attributes, option models, lease models, holder abstractions, serializers, and extension methods used by both client and server packages.

## Entry Points

- `Interfaces/` — public grain interfaces and configuration contracts.
- `Attributes/` — limiter metadata used by client middleware and server filters.
- `Models/` — rate-limit leases, config, metadata, and holder types.
- `Surrogates/` — Orleans serialization surrogates and converters for rate limiter options/statistics.
- `Extensions/` — shared service and grain-factory helpers.

## Boundaries

- Core must stay transport-agnostic and hosting-agnostic.
- Do not add ASP.NET Core middleware, SignalR, or silo implementation logic here.
- Keep public contract changes backwards-compatible unless the root task explicitly covers a breaking change.
- Serialization changes must preserve Orleans compatibility and be covered by tests.

## Commands

- `build`: `dotnet build ../ManagedCode.Orleans.RateLimiting.sln --configuration Release --no-restore`
- `test`: `dotnet test --solution ../ManagedCode.Orleans.RateLimiting.sln --configuration Release --no-build --verbosity normal`

## Applicable Skills

- `dotnet`
- `modern-csharp`
- `microsoft-extensions`
- `orleans`
- `code-analysis`
- `complexity`

## Protected Areas

- Grain interface signatures.
- Attribute constructor behaviour and key semantics.
- Orleans surrogate/converter pairs.
- Lease metadata names and retry-after semantics.
