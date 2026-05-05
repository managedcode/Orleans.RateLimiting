# AGENTS.md

Project: ManagedCode.Orleans.RateLimiting
Stack: .NET 10, Orleans 10, ASP.NET Core middleware and SignalR, TUnit, Shouldly

Follows [MCAF](https://mcaf.managed-code.com/).

## Purpose

This file defines how AI agents work in this solution.

- The root `AGENTS.md` holds global workflow, shared commands, cross-cutting rules, and repo-level preferences.
- This is a multi-project solution. Each project root has its own local `AGENTS.md`.
- Local files refine ownership, boundaries, entry points, and project-specific risks.

## Solution Topology

- Solution root: `/Users/ksemenenko/Developer/Orleans.RateLimiting`
- Solution file: `ManagedCode.Orleans.RateLimiting.sln`
- Architecture map: `docs/Architecture.md`
- Local agent files:
  - `ManagedCode.Orleans.RateLimiting.Core/AGENTS.md`
  - `ManagedCode.Orleans.RateLimiting.Client/AGENTS.md`
  - `ManagedCode.Orleans.RateLimiting.Server/AGENTS.md`
  - `ManagedCode.Orleans.RateLimiting.Tests/AGENTS.md`

## Rule Precedence

1. Read this root file first.
2. Read `docs/Architecture.md` for non-trivial behaviour or architecture changes.
3. Read the nearest local `AGENTS.md` for the project you will edit.
4. Apply the stricter rule when root and local rules overlap.
5. Document any exception in the nearest local `AGENTS.md`, an ADR, or the task plan before relying on it.

## Conversations

Record durable user preferences here when they are stable across tasks. Do not record one-off implementation details.

Update this file when the user gives a repeated correction, a permanent workflow change, a lasting preference, or a strong negative signal that a rule was missed.

## Global Skills

Use the installed Managed Code Skills catalog entries that match this repo:

- `dotnet` — .NET SDK, MSBuild, NuGet, project structure, and package management work.
- `project-setup` — SDK pinning, central package management, repo-level build files, and tool manifests.
- `modern-csharp` — C# language features and idioms for the configured SDK.
- `microsoft-extensions` — dependency injection, options, hosting, logging, and configuration.
- `orleans` — grains, grain interfaces, silo/client setup, filters, and serialization.
- `quality-ci` — GitHub Actions quality gates, build/test/analyze/coverage workflow work.
- `format`, `csharpier`, `roslynator`, `analyzer-config`, `code-analysis` — formatting and analyzer alignment.
- `complexity`, `crap-score` — maintainability and complexity review.
- `architecture` — architecture map, boundaries, and module-level reasoning.

Skill-management rules:

- .NET skills are sourced from `https://skills.managed-code.com/`.
- MCAF framework skills, when installed, must use current `mcaf-*` skill ids only.
- Do not add obsolete skill ids from old tutorials unless the current catalog reintroduces them.
- Tool-specific skills belong here only when the repository actually uses the tool locally or in CI.
- Keep one test-framework direction for this repo: TUnit with Shouldly.
- After skill updates, recheck build, test, format, analyze, complexity, and coverage commands against the repo toolchain.

## Commands

- `restore`: `dotnet restore ManagedCode.Orleans.RateLimiting.sln`
- `build`: `dotnet build ManagedCode.Orleans.RateLimiting.sln --configuration Release --no-restore`
- `analyze`: `dotnet build ManagedCode.Orleans.RateLimiting.sln --configuration Release --no-restore -p:RunAnalyzers=true`
- `format`: `dotnet format ManagedCode.Orleans.RateLimiting.sln --verify-no-changes`
- `test`: `dotnet test --solution ManagedCode.Orleans.RateLimiting.sln --configuration Release --no-build --verbosity normal`
- `coverage`: `dotnet tool restore` then `dotnet tool run coverlet ManagedCode.Orleans.RateLimiting.Tests/bin/Release/net10.0/ManagedCode.Orleans.RateLimiting.Tests.dll --target "dotnet" --targetargs "test --project ManagedCode.Orleans.RateLimiting.Tests/ManagedCode.Orleans.RateLimiting.Tests.csproj --configuration Release --no-build --no-restore" --format cobertura --output artifacts/coverage/coverage.cobertura.xml --exclude "[ManagedCode.Orleans.RateLimiting.Tests]*" --threshold 85 --threshold-type line --threshold-stat total`
- `coverage-report`: `dotnet tool run reportgenerator -reports:"artifacts/coverage/coverage.cobertura.xml" -targetdir:"artifacts/coverage-report" -reporttypes:"HtmlSummary;MarkdownSummaryGithub"`
- `pack`: `dotnet pack ManagedCode.Orleans.RateLimiting.sln --configuration Release --no-build`

.NET-specific rules:

- The target framework is configured centrally in `Directory.Build.props`.
- Package versions are configured centrally in `Directory.Packages.props`.
- Tests run through Microsoft.Testing.Platform with TUnit.
- Assertions use Shouldly.
- Do not add xUnit, NUnit, MSTest, or FluentAssertions to new code.
- Keep local tools in `.config/dotnet-tools.json`.

## Project AGENTS Policy

- Keep one root `AGENTS.md` and one local `AGENTS.md` per project root.
- Each local file must document purpose, entry points, boundaries, local commands, applicable skills, and protected areas.
- Tighten a local file before making a change that would otherwise make the root guidance vague.

## Maintainability Limits

- `file_max_loc`: `400`
- `type_max_loc`: `200`
- `function_max_loc`: `50`
- `max_nesting_depth`: `3`
- `line_coverage_target`: `85%`
- `critical_contract_line_coverage_target`: `90%`
- `exception_policy`: document any justified exception in the nearest ADR, feature doc, task plan, or local `AGENTS.md` with reason, scope, and a removal or refactor plan.

## Task Delivery

- Start non-trivial work from `docs/Architecture.md` and the nearest local `AGENTS.md`.
- Define scope before coding: in scope, out of scope, risks, and expected verification.
- Use vertical slices when adding behaviour: keep contracts, implementation, tests, and docs close to the feature boundary.
- Prefer the smallest relevant project area over broad repo scanning.
- Implement code and tests together.
- For new behaviour and bug fixes, write or update automated tests that prove caller-visible behaviour.
- Run verification in layers: changed tests, related suite, build, analyze, format, coverage when production behaviour changed.
- The task is complete only when the relevant checklist is done and verification is green, or a documented exception explains the remaining gap.

## Documentation

- Durable docs live under `docs/`.
- `docs/Architecture.md` is the required global map and first stop for non-trivial work.
- Architecture docs, feature docs, and ADRs should include Mermaid diagrams when they describe behaviour, boundaries, or decisions.
- Update README/package docs when public APIs, setup, package metadata, or user-facing behaviour changes.
- Update ADRs when architecture, boundaries, or standards change.

## Testing

- TDD is preferred for new behaviour and bug fixes.
- Bug fixes start with a failing regression test when practical.
- Tests should prove real Orleans, middleware, HTTP, or SignalR flows through public contracts.
- Prefer TestServer, Orleans TestingHost, and real in-process dependencies over mocks.
- Flaky tests are failures; fix the cause.
- Do not lower coverage, analyzer severity, or assertion strength to make a branch green.

## Code and Design

- Follow SOLID principles and strong cohesion by default.
- Keep public contracts explicit and stable.
- Prefer composition over inheritance unless inheritance is already the local pattern or is justified.
- Avoid magic values. Use named constants, options, value objects, or documented defaults.
- Do not introduce raw string or numeric literals in code. Extract them to named constants, static readonly fields, options, or documented defaults before use.
- Keep serialization and Orleans compatibility in mind when changing grain contracts, attributes, models, or surrogates.
- Avoid adding dependencies unless the value is clear and the dependency belongs at the affected layer.

## Source Control

- Work on a branch with prefix `codex/` unless the user requests another branch.
- The agent may commit only when the user asks for or allows commits.
- Never push unless the user explicitly asks. The user prefers to push themselves.
- Never rewrite history or force-push without explicit instruction.
- Do not revert user changes unless the user explicitly requests it.

## Boundaries

Always:

- Read root and local `AGENTS.md` files before editing code.
- Read relevant docs before changing behaviour or architecture.
- Run the required verification commands yourself when feasible.

Ask first:

- changing public API contracts in a breaking way
- adding new runtime dependencies
- deleting project files or large code areas
- changing package identity, license, signing, or publishing settings

## Preferences

### Likes

- Use .NET 10, central package management, and centralized repo settings.
- Use TUnit and Shouldly for tests.
- Keep CI and package metadata aligned with the ManagedCode style.
- Keep agent guidance concrete and repo-specific instead of leaving template placeholders.

### Dislikes

- Do not push branches from the agent unless explicitly requested.
- Do not introduce xUnit or FluentAssertions in new test work.
- Do not leave AGENTS template placeholders or placeholder paths.
- Do not add string or numeric literals directly in code; use named constants instead.
