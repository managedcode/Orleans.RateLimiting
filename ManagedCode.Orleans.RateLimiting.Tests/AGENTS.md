# AGENTS.md

Project: ManagedCode.Orleans.RateLimiting.Tests

## Purpose

Contains integration and behaviour tests for grain limiters, HTTP middleware, SignalR hub filters, and the in-process Orleans/test-server host used by the suite.

## Entry Points

- `Cluster/TestClusterApplication.cs` — shared in-process Orleans and HTTP test host.
- `Cluster/TestSiloConfigurations.cs` and `Cluster/TestClientConfigurations.cs` — test cluster wiring.
- `Cluster/Grains/` — test grain contracts and implementations.
- `TestApp/` — ASP.NET Core test app, controller, and SignalR hub.
- `*Tests.cs` — TUnit test classes grouped by limiter or integration surface.

## Boundaries

- Tests use TUnit with Microsoft.Testing.Platform and Shouldly.
- Do not add xUnit, NUnit, MSTest, or FluentAssertions.
- Prefer real in-process TestServer and Orleans TestingHost flows over mocks.
- Keep test data deterministic; avoid timing assumptions unless the limiter behaviour requires them.

## Commands

- `build`: `dotnet build ../ManagedCode.Orleans.RateLimiting.sln --configuration Release --no-restore`
- `test`: `dotnet test --project ManagedCode.Orleans.RateLimiting.Tests.csproj --configuration Release --no-build --verbosity normal`
- `coverage`: run the root coverage command from `../AGENTS.md` after a Release build.

## Applicable Skills

- `dotnet`
- `modern-csharp`
- `orleans`
- `quality-ci`
- `code-analysis`

## Protected Areas

- Shared cluster lifecycle and test isolation.
- `NotInParallel` assembly-level setting while the suite shares cluster resources.
- HTTP 429 assertions and retry-after metadata checks.
- SignalR invocation limiting scenarios.
