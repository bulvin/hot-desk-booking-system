# Requirements: Upgrade dependencies for .NET 10

Refresh the supporting libraries covered by [roadmap Phase 3](../roadmap.md#phase-3-upgrade-dependencies-for-net-10).

## Required outcomes

- Reviewed packages work with .NET 10 and have documented support, security, and licensing decisions.
- Unused packages are removed after checking their usage.
- The AutoMapper security finding reported in the previous phase is reviewed and resolved against current audit results.
- Validation, request dispatch, response mapping, Swagger, login, and role permissions keep working.
- Existing password hashes remain usable. API response shapes and validation errors remain consistent.
- The full test suite runs and produces readable test and coverage reports.

Pay particular attention to MediatR and AutoMapper licensing before selecting versions. Record decisions in [plan.md](plan.md).

## Scope limits

Keep the current architecture and one-to-seven-day booking rules. Formatting, analyzers, CI, new endpoints, and booking fixes belong to later phases.

Keep the completed SDK, framework, database, and container upgrades unless a demonstrated compatibility or security issue requires a change. Use disposable databases for verification and preserve existing developer data.

## Completion

The phase is complete when the dependency decisions are recorded and all checks in [validation.md](validation.md) pass. Record unresolved issues explicitly.
