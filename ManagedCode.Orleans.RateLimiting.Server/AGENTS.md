# AGENTS.md

Project: ManagedCode.Orleans.RateLimiting.Server

## Purpose

Implements Orleans grains, grain call filters, and silo registration helpers for distributed rate limiting backed by .NET rate limiter primitives.

## Entry Points

- `Grains/` — concrete limiter grain implementations.
- `GrainCallFilter/` — incoming call filters that enforce limiter attributes on grain calls.
- `Extensions/` — silo builder registration helpers for limiter types.
- `Properties/AssemblyInfo.cs` — Orleans application-part metadata.

## Boundaries

- Server may depend on Core and Orleans server packages.
- Do not add ASP.NET Core middleware or HTTP response shaping here.
- Grain APIs must stay aligned with Core interfaces.
- Replenishment, partitioning, and limiter option behaviour must be deterministic and test-covered.

## Commands

- `build`: `dotnet build ../ManagedCode.Orleans.RateLimiting.sln --configuration Release --no-restore`
- `test`: `dotnet test --project ../ManagedCode.Orleans.RateLimiting.Tests/ManagedCode.Orleans.RateLimiting.Tests.csproj --configuration Release --no-build --verbosity normal`

## Applicable Skills

- `dotnet`
- `modern-csharp`
- `orleans`
- `microsoft-extensions`
- `complexity`
- `code-analysis`

## Protected Areas

- Grain state and limiter lifecycle.
- Incoming grain call filter ordering and key selection.
- Silo builder extension defaults.
- Rate limiter option mapping to .NET runtime limiter implementations.
