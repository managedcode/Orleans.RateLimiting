# Project Review

Date: 2026-05-05

## Findings

1. `ManagedCode.Orleans.RateLimiting.Client/Middlewares/RateLimitingHubFilter.cs`
   The SignalR filter is present but not registered by the public `AddOrleansRateLimiting` extension. The current tests cover SignalR connection startup, not limiter enforcement through hub invocation. Next step: decide whether this package should automatically register the filter, expose an explicit registration extension, or document manual registration.

2. `ManagedCode.Orleans.RateLimiting.Core/Models/OrleansRateLimitLease.cs`
   Dispose paths still raise analyzer warnings around fire-and-forget `ValueTask` usage and `GC.SuppressFinalize`. This is non-blocking for current tests, but it should be tightened because leases are a core lifecycle primitive.

3. `ManagedCode.Orleans.RateLimiting.Core/Surrogates/*`
   Orleans surrogate types intentionally expose fields for serialization, but analyzers report `CA1051`. Either document this as a serialization exception in `.editorconfig` or convert to serializer-compatible properties if Orleans 10 supports the shape cleanly.

4. Project files
   `NU1510` reports that several direct package references may be unnecessary under .NET 10 package pruning. Keep the explicit references if they are part of the public compile surface; otherwise prune them after checking package assets and generated NuGet dependency groups.

## Improvements Completed

- Target framework and language settings are centralized for .NET 10 and C# 14.
- NuGet versions are centralized in `Directory.Packages.props`.
- Test stack is TUnit with Shouldly and Microsoft.Testing.Platform.
- CI and NuGet workflows use .NET 10 and current GitHub Actions.
- Coverage is enforced at 85% total line coverage in the CI coverage command.
- Root and project-local `AGENTS.md` files describe repository workflow and ownership.
- `docs/Architecture.md` now documents module boundaries and request flow.
- Request orchestration extension points were added for user, group, tenant, role, IP, endpoint, grain, and custom partitions.
- Fixed a real routing bug in `GetRateLimiter<T>` where the generic interface switch could not match the intended limiter interfaces.
- Fixed option-backed holder acquisition so the stored option is applied when the caller uses the `permitCount` overload.

## Verification Snapshot

- Build: passed on .NET SDK 10.0.201.
- Format: `dotnet format ManagedCode.Orleans.RateLimiting.sln --verify-no-changes` passed with a workspace-load warning only.
- Tests: 28 passed, 0 failed.
- Coverage: 91.02% line, 80.82% branch, 91.82% method.
- Pack: Core, Client, and Server packages were created as `10.0.0` packages in `artifacts/packages`.
