# Validation: Upgrade dependencies for .NET 10

Status: passed on 2026-10-06.

## Results

- [x] Baseline: 3 tests passed; AutoMapper security warning and two nullable warnings.
- [x] Final Release build and tests: 14 passed, zero failed or skipped. Only the two existing nullable warnings remain.
- [x] Final NuGet audit: no vulnerable or deprecated direct or transitive packages across all five projects.
- [x] Validation runs before handlers; invalid requests stop, requests without validators dispatch, and cancellation reaches validators.
- [x] Explicit DTO mappings preserve IDs, fields, null descriptions, dates, status, and pagination.
- [x] BCrypt 4.0.3 fixture hashes and new hashes verify. Wrong passwords, wrong JWT signatures, and expired tokens are rejected.
- [x] Docker image builds. Swagger JSON/UI, routes, date schema, and bearer configuration pass.
- [x] HTTP checks pass: registration/login/list 200; location/desk/booking creation 201; invalid login 400; invalid registration 422; unauthenticated/invalid-token 401; employee admin-action 403.
- [x] Reservation details hide identity from employees and include it for admins.
