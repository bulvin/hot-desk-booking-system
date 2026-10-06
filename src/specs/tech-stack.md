# Tech stack

Source inventory: 2026-10-06. Versions below come from project files, not a resolved dependency or vulnerability audit. Build and tests have not been run as part of this documentation task.

## Architecture

Retain the existing four layers:

- `Domain`: entities, repository contracts, domain exceptions, and unit-of-work contract.
- `Application`: command/query handlers, DTOs, mapping profiles, validation, and service contracts.
- `Infrastructure`: EF Core persistence, PostgreSQL repositories, migrations, authentication, and time services.
- `Web.Api`: ASP.NET Core controllers, dependency composition, HTTP error handling, and Swagger.

Requests use MediatR with a validation pipeline; persistence uses repositories and a unit of work. Application currently also uses HTTP context to identify users. Review coupling incrementally when it affects testing, without making an architecture rewrite a migration prerequisite.

The solution is `src/hot-desk-booking-system.sln`; tests live in the sibling `tests/UnitTests` directory. Future shared build and analyzer configuration must cover both trees, not only `src`.

## Current inventory and direction

| Component | Current source configuration | Refresh direction |
| --- | --- | --- |
| Runtime | All four projects and UnitTests target `net8.0` | Target `net10.0`; pin a reviewed SDK in `global.json` |
| API | ASP.NET Core controllers | Retain controller-based REST API |
| Database | PostgreSQL container image `postgres:17` | Retain PostgreSQL; review image patches separately from runtime upgrade |
| Persistence | Npgsql EF provider `9.0.0-rc.2`; EF tools `9.0.0-rc.2.24474.1` | Move to compatible stable EF Core/Npgsql 10 packages and tooling together |
| Dispatch | MediatR `12.4.1` | Review upgrade compatibility, license terms, and value before selecting a version |
| Mapping | AutoMapper `13.0.1` | Review upgrade and licensing impact; retain or replace only with a documented reason |
| Validation | FluentValidation.AspNetCore `11.3.0`; DI extensions `11.10.0` | Keep pipeline validation; remove unsupported ASP.NET integration if unused and use compatible core/DI packages |
| Authentication | JwtBearer `8.0.10`; BCrypt.Net-Next `4.0.3` | Align JWT package with .NET 10; independently review password-hashing dependency |
| API documentation | Microsoft.AspNetCore.OpenApi `8.0.8`; Swashbuckle `6.4.0`; annotations `6.9.0` | Review actual usage and align retained packages; verify Swagger and auth definitions |
| Unit tests | xUnit `2.5.3`, runner `2.5.3`, Moq `4.20.72` | Review compatible upgrades independently of test redesign |
| Test tooling | Microsoft.NET.Test.Sdk `17.8.0`, coverlet.collector `6.0.0` | Update compatible test tooling and verify discovery and coverage collection |
| Containers | .NET SDK/runtime images `8.0`, Docker Compose | Move both .NET images to 10.0 and smoke-test the image |

The reviewed machine has SDKs 8.0.100, 9.0.300, and 10.0.400 installed. This is an environment observation, not an SDK version policy.

## Dependency review policy

For each direct dependency, inspect transitive dependencies, supported targets, breaking changes, maintenance status, vulnerabilities, and license terms. Record a keep/update/remove/replace decision and a verification step. Select exact stable versions during implementation rather than assuming the newest major release is suitable.

Npgsql publishes an [EF Core provider 10 release](https://www.npgsql.org/efcore/release-notes/10.0.html). The [FluentValidation.AspNetCore repository](https://github.com/FluentValidation/FluentValidation.AspNetCore) states that package is unsupported; the current application already registers a MediatR validation behavior. [AutoMapper's 15 upgrade guide](https://docs.automapper.io/en/latest/15.0-Upgrade-Guide.html) introduces license requirements, which makes a blind major-version update inappropriate.

## Static analysis and maintenance target

- Start with SDK-provided .NET analyzers, compiler/nullability diagnostics, and a shared `.editorconfig`.
- Pin the analyzer level to the chosen .NET 10 policy; configure intentional severities instead of allowing SDK updates to silently change the rule set.
- Enable build-time code-style checks where useful and enforce formatting through `dotnet format --verify-no-changes`.
- Introduce warnings as errors after reviewing and fixing the baseline, with narrow, explained suppressions where warranted.
- Keep generated migration output separate from handwritten-code style cleanup.
- Assess additional analyzer packages only when they address a demonstrated gap; avoid overlapping rule sets by default.
- Automate restore/build/test/format checks, dependency vulnerability review, and periodic dependency update proposals in CI.

Microsoft documents [analysis configuration](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files) and [analysis levels and build-time style rules](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview).

## Runtime and data considerations

The application applies EF migrations and seeds data at startup. Review that lifecycle before relying on it in automated tests or deployment. Preserve migration history and assume data should be retained until its disposability is explicitly established.

Development uses .NET User Secrets and a Docker secret file for PostgreSQL. Document required configuration keys and setup steps without committing secret values. Verify JWT validation, administrator bootstrap, and role enforcement during regression checks.

Use the [.NET 10 compatibility notes](https://learn.microsoft.com/en-us/dotnet/core/compatibility/10.0), [ASP.NET Core migration guides](https://learn.microsoft.com/en-us/aspnet/core/migration/), and [EF Core 10 breaking changes](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/breaking-changes) during migration. Review intervening .NET 9 changes as well, even if the application moves directly from 8 to 10.
