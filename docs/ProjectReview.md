# Project Review

Date: 2026-05-05

## Findings

1. `ManagedCode.Orleans.RateLimiting.Client/Middlewares/OrleansIpRateLimitingMiddleware.cs` and `ManagedCode.Orleans.RateLimiting.Client/Middlewares/OrleansUserRateLimitingMiddleware.cs`
   Attribute-based HTTP middleware is still present for existing users, but new applications should prefer `UseOrleansRequestRateLimiting` with orchestration rules. Next major version can remove or mark the attribute-specific middleware as legacy once consumers move to request orchestration.

2. `ManagedCode.Orleans.RateLimiting.Server/Grains/RateLimiterGrain.cs`
   Limiter state is in-memory per Orleans grain activation. That is correct for distributed runtime coordination, but it is not a durable quota ledger. Document this clearly for users who expect persistence across silo restarts.

3. `ManagedCode.Orleans.RateLimiting.Core/Interfaces/IRateLimitRequestPolicy.cs`
   The orchestration API now supports static option rules and custom policies. A useful next feature is named policy selection per endpoint or hub method so applications can register multiple independent policy sets instead of one global rule collection.

## Improvements Completed

- Target framework and language settings are centralized for .NET 10 and C# 14.
- NuGet versions are centralized in `Directory.Packages.props`.
- Test stack is TUnit with Shouldly and Microsoft.Testing.Platform.
- CI and NuGet workflows use .NET 10 and current GitHub Actions.
- Coverage is enforced at 85% total line coverage in the CI coverage command.
- Root and project-local `AGENTS.md` files describe repository workflow and ownership.
- `docs/Architecture.md` documents module boundaries, request orchestration, and request flow.
- Request orchestration now has default implementations: options-backed policy, key resolver, and orchestrator.
- Added first-class request partitions for user, group, tenant, role, IP address, endpoint, grain, and custom metadata keys.
- Added `UseOrleansRequestRateLimiting` for ASP.NET Core request pipelines.
- Reworked SignalR rate limiting to use the orchestration pipeline and public SignalR registration.
- Removed commented-out partitioned/replenishing grain placeholders.
- Switched Core and Server projects from Web SDK to library SDK.
- Removed unnecessary Client package references after .NET 10 pruning.
- Converted Orleans surrogate fields to serializer properties.
- Fixed lease lifecycle issues: rejected leases are not retained, group acquisition releases partial successes, and dispose paths are no longer fire-and-forget.
- Fixed a routing bug in `GetRateLimiter<T>` where the generic interface switch could not match the intended limiter interfaces.
- Fixed option-backed holder acquisition so the stored option is applied when the caller uses the `permitCount` overload.

## Verification Snapshot

- Build: passed on .NET SDK 10.0.201 with 0 warnings.
- Restore: passed with central package management.
- Tests: 31 passed, 0 failed.
- Coverage: 88.02% line, 74.52% branch, 91.13% method.
- Pack: Core, Client, and Server packages were created as `10.0.0` packages in `artifacts/packages`.
