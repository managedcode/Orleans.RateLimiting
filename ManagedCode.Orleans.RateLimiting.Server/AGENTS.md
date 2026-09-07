# AGENTS.md

Project: ManagedCode.Orleans.RateLimiting.Server

## Purpose

Implements Orleans grains, grain call filters, and silo registration helpers for distributed rate limiting backed by .NET rate limiter primitives.

## Entry Points

- `Grains/` — concrete limiter grain implementations.
- `GrainCallFilter/` — incoming attribute enforcement.
- `Services/` — acquisition timeout provider backed by silo messaging options.
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

- `OrleansContracts.txt` records the published RPC identities; regenerate with the root command, review every diff, and retain retired history.

- Grain state and limiter lifecycle.
- Incoming grain call filter ordering and key selection.
- Silo builder extension defaults.
- Rate limiter option mapping to .NET runtime limiter implementations.
- Bounded acquisition fast paths, native queue cleanup, and explicit concurrency-lease release.
