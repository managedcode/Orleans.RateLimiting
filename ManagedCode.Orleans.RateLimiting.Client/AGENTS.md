# AGENTS.md

Project: ManagedCode.Orleans.RateLimiting.Client

## Purpose

Provides ASP.NET Core and SignalR integration that resolves rate-limiter metadata from requests, users, roles, IP addresses, hubs, and attributes, then calls Orleans grains through the shared Core contracts.

## Entry Points

- `Middlewares/OrleansBaseRateLimitingMiddleware.cs` — common HTTP rate-limiting response flow.
- `Middlewares/OrleansIpRateLimitingMiddleware.cs` — IP-based HTTP limiting.
- `Middlewares/OrleansUserRateLimitingMiddleware.cs` — authenticated user and role based HTTP limiting.
- `Middlewares/RateLimitingHubFilter.cs` — SignalR hub invocation limiting.
- `Attributes/` — ASP.NET-facing limiter attributes.
- `Extensions/` — registration and pipeline helpers.

## Boundaries

- Client may depend on Core and Orleans client abstractions.
- Do not implement grain state or silo-only concerns here.
- Middleware must return stable, useful 429 responses with retry metadata when available.
- Request key extraction must be explicit and test-covered for anonymous, authenticated, role, and IP flows.

## Commands

- `build`: `dotnet build ../ManagedCode.Orleans.RateLimiting.sln --configuration Release --no-restore`
- `test`: `dotnet test --project ../ManagedCode.Orleans.RateLimiting.Tests/ManagedCode.Orleans.RateLimiting.Tests.csproj --configuration Release --no-build --verbosity normal`

## Applicable Skills

- `dotnet`
- `modern-csharp`
- `microsoft-extensions`
- `orleans`
- `quality-ci`
- `code-analysis`

## Protected Areas

- Public extension method names and overloads.
- HTTP status code and response body contract for rejected requests.
- SignalR hub filter behaviour.
- Identity and IP key-selection semantics.
