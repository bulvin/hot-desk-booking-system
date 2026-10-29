# HotDeskBooking | ASP.NET Core 10, Entity Framework Core, and PostgreSQL 18. ASP.NET Core Identity manages local accounts; JWTs and rotating refresh sessions provide authentication.

Before making changes, read the [mission](specs/mission.md) for scope and agreed business rules. The [tech stack](specs/tech-stack.md) describes implementation choices, and the [roadmap](specs/roadmap.md) defines delivery order. The current priority is stabilizing the existing API. Specialized instructions are available under `.agents/skills/`.

## Commands

Run from the repository root. `global.json` defines the SDK; `README.md` describes local configuration and secrets.

```powershell
dotnet build
dotnet test
dotnet format
dotnet ef migrations add <name> -p src/Infrastructure
```

Start locally after configuring dependencies:

```powershell
dotnet run
```

## Architecture

Preserve the four layers and existing dependency directions:

- `src/Domain`: entities, core concepts, and domain rules.
- `src/Application`: use cases, commands and queries, handlers, and validation.
- `src/Infrastructure`: EF Core, repositories, migrations, the clock, and external service integrations.
- `src/Web.Api`: HTTP handling, application configuration, response contracts, and Problem Details.
- `tests/UnitTests`: tests using xUnit and Moq and WebApplicationFactory; database integrity and concurrency tests require real PostgreSQL.

Use the existing source-generated Mediator and explicit DTO mapping. Place use-case logic in the appropriate layer following existing patterns.

## Conventions

- `.editorconfig` and `Directory.Build.props` define formatting and analysis: four spaces, file-scoped namespaces, interfaces prefixed with `I`, PascalCase types and properties, and `_camelCase` private fields.
- Preserve nullable reference types and warnings as errors. Diagnostic suppressions must be narrow and justified.
- Use existing FluentValidation validators, clock abstractions, and test patterns.
- FluentValidation, not in endpoints
- Keep HTTP statuses, Problem Details, and Swagger documentation consistent.
- Git Conventional Commits

## What not to do

- Do not expand routine refresh work to new endpoints, frontend development, multi-company tenancy, hourly or recurring bookings, notifications, reporting, or production deployment. These require separately agreed scope; an explicit user request can change that scope.
- Do not independently decide the business timezone, desk-change cutoff, multiple same-day employee reservation policy, or how completed/canceled reservations affect availability and desk deletion. Ask for the relevant decision when implementation depends on it.
- Do not treat an application-level overlap check as protection against concurrency. A database safeguard remains a requirement to implement.

- Do not commit secrets or expose them in logs.
- Do not delete volumes or data as a PostgreSQL upgrade strategy; preserve records and migration history. Do not rewrite existing migrations during formatting.
- Do not bypass failures by globally disabling analyzers or warnings.
- Do not bypass tests
- Do not add NuGet packages without ask

## Done when

- dotnet build without warnings
- dotnet test on green color
- The change fulfills the agreed scope and rules in `specs/mission.md`, preserving the four-layer architecture.
- Appropriate checks pass. For code, dependency, or CI changes, run the restore, Release build, tests, and formatting verification commands above. Analyzers run during the build.
- Behavior changes have meaningful regression coverage. Changes protecting against concurrent bookings verify the invariant against PostgreSQL.
- HTTP contract changes keep responses, Swagger, authorization, and privacy consistent.
- Database changes preserve data and migration history; configuration changes include current, reproducible local startup instructions.
- Dependency decisions are recorded, and the completion report identifies checks performed and unresolved limitations. Documentation-only changes require content, path, and diff checks.
