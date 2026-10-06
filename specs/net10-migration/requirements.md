# Requirements: .NET 10 migration

Status: implemented and validated on 2026-10-06.

Roadmap: [Phase 1: Run the solution on .NET 10](../roadmap.md#phase-1-run-the-solution-on-net-10).
Branch: `chore/net10-migration`.

## Objective

Run the complete solution on .NET 10 with a compatible dependency graph, preserving the existing architecture, HTTP contracts, and inclusive 1-7-day reservation model.

## Agreed decisions

- Use `specs/net10-migration/` for this technical migration slice.
- Limit implementation to SDK/framework upgrades, compatible EF Core/Npgsql packages and tooling, and fixes necessary for the migration.
- Require successful restore, Release build, and all existing tests passing before merge.
- Correct stale test expectations when they contradict intended behavior; do not weaken assertions or skip tests to obtain a passing result.
- Keep Docker updates and independent library modernization in their later roadmap phases.

## Scope

1. Pin a reviewed stable .NET 10 SDK using `global.json` at the repository root so it covers both `src` and sibling `tests`. Record the exact SDK and intentional roll-forward policy.
2. Retarget `Domain`, `Application`, `Infrastructure`, `Web.Api`, and `tests/UnitTests` to `net10.0`.
3. Align framework-coupled package references, including JwtBearer and the retained Microsoft OpenAPI package, with .NET 10. Replace prerelease EF Core tooling and Npgsql EF provider references with compatible stable 10.x versions; inspect resolved transitive dependencies for conflicts.
4. Review applicable .NET 9 and .NET 10 breaking changes and fix compilation or test failures attributable to the migration. If an otherwise deferred package blocks compatibility, make only the required adjustment and record why.
5. Repair stale existing tests using source and intended behavior as evidence. Fix a discovered application defect only when necessary for this slice; record unrelated defects for their later phase.

## Constraints

- Preserve layer boundaries and existing endpoint behavior; no business-rule redesign or broad formatting cleanup.
- Preserve database migration history and existing data. Do not reset databases or add schema changes for a framework retarget alone.
- Preserve unrelated working-tree edits, including the existing solution-file edit.
- Review compatibility, security, maintenance, and licensing for packages changed in this slice. Choose exact versions during implementation and record the decisions here.
- Shared SDK configuration and the test project are outside `src`; implementation must account for repository-root filesystem permissions.

## Deferred

Docker image updates, live database/migration verification, and container smoke tests belong to Phase 2. General MediatR, AutoMapper, FluentValidation, Swagger, authentication, and test-tool modernization remain in their respective phases. Static analysis, CI, new endpoints, and booking correctness improvements are not part of this slice.

## Implementation decision record

Versions were selected from official NuGet package metadata and verified against the resolved dependency graph.

| Decision | Selected value and rationale |
| --- | --- |
| .NET 10 SDK and roll-forward policy | 10.0.400; `latestPatch` within the selected feature band; prerelease SDKs disabled. Resolves from repository root and sibling tests. |
| Framework package versions | JwtBearer 10.0.12. Removed the unused Microsoft.AspNetCore.OpenApi reference; existing Swagger remains. |
| Stable EF Core/Npgsql/tooling versions | EF Core Relational and Tools 10.0.12; Npgsql EF provider and driver 10.0.3. A direct Relational reference aligns runtime dependencies across API, infrastructure, and tests with the tools' patch version. |
| Relevant breaking changes and fixes | Upgraded OpenAPI package pulled incompatible OpenAPI.NET types into existing Swagger code; removing the unused package fixes compilation. EF runtime/tooling initially resolved different patches; explicit Relational alignment removes MSB3277 conflicts. |
| Stale tests corrected, with evidence | Missing-desk test expected ApplicationException and an obsolete message. The unchanged handler throws DeskNotFoundException. The test now asserts that exact type, the desk ID, 404 status, and current message; nullable mock result corrected. All three cases pass. |
| Required exceptions for deferred dependencies | Only unused Microsoft.AspNetCore.OpenApi removal was necessary. MediatR, AutoMapper, FluentValidation, Swashbuckle, BCrypt, and test tooling retain their versions. |

## Compatibility and maintenance review

- The changed Microsoft packages declare MIT licensing; the Npgsql provider declares the PostgreSQL license in its NuGet metadata. Stable versions restore and resolve without downgrade or assembly-conflict warnings after alignment.
- Reviewed [ASP.NET Core 9 changes](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-9.0) and [ASP.NET Core 10 changes](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0). The observed OpenAPI dependency conflict is addressed without migrating the Swagger implementation.
- Reviewed [EF Core 9 breaking changes](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-9.0/breaking-changes), [EF Core 10 breaking changes](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/breaking-changes), and [Npgsql provider 10 notes](https://www.npgsql.org/efcore/release-notes/10.0.html). Projects use a single target framework; SQL Server-specific changes do not apply. Startup migration behavior, pending-model checks, and database query behavior still require Phase 2 verification; no suppression or schema workaround was added.
- Restore reports the pre-existing high-severity AutoMapper 13.0.1 advisory [GHSA-rvv3-g6hj-g44x](https://github.com/advisories/GHSA-rvv3-g6hj-g44x). It remains visible and unsuppressed for Phase 3 review; this migration is not a clean security audit or production-readiness sign-off.
- Two pre-existing CS8602 warnings in GetDesksValidator remain for later validation/static-analysis work. No production behavior was changed to address unrelated warnings.

See [plan.md](plan.md) for tasks and [validation.md](validation.md) for merge criteria.
