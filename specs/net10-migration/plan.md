# Plan: .NET 10 migration

Status: implemented and validated on 2026-10-06. Each numbered group has at most three tasks. Scope is defined in [requirements.md](requirements.md); verification evidence belongs in [validation.md](validation.md).

## 1. Select a compatible upgrade set

- [x] Inspect current project/package references and run the existing build/tests to distinguish pre-existing failures from migration failures.
- [x] Review official .NET 9/10 and EF/Npgsql breaking changes; select stable compatible SDK, framework, persistence, and tooling versions.
- [x] Record exact versions, the SDK roll-forward policy, and relevant compatibility/license decisions in `requirements.md`.

Outcome: a concrete compatible upgrade set and an explanation of existing failures, without a separate repository-wide audit phase.

## 2. Retarget the complete solution

- [x] Add repository-root `global.json` and retarget all four production projects and sibling `tests/UnitTests` to `net10.0`.
- [x] Align framework packages and replace prerelease EF/Npgsql references with the reviewed stable versions as one compatible change.
- [x] Restore and build; resolve migration-related code issues and only strictly necessary compatibility changes to otherwise deferred dependencies.

Outcome: the full solution restores and builds in Release on the selected .NET 10 SDK.

## 3. Verify behavior and prepare for merge

- [x] Run all existing tests, correct stale expectations with documented evidence, and rerun after any fixes without disabling tests.
- [x] Execute the final checks in `validation.md` and record SDK/version selection, test totals, warnings, results, and any limitations.
- [x] Review the final diff for scope and unrelated changes; mark roadmap Phase 1 complete only after its merge criteria pass.

Outcome: a reviewable migration with passing restore/build/test checks and explicit evidence. Database runtime and Docker verification follow in Phase 2.
