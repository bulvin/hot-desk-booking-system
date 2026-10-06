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

Docker checks used PostgreSQL 18 with temporary memory-backed storage, unique container/network names, and a random loopback port. Test containers and network were removed. Existing developer data was untouched.

## Reproduce

Run from the repository root with the pinned SDK and Docker Desktop running.

```powershell
dotnet restore hot-desk-booking-system.sln
dotnet build hot-desk-booking-system.sln -c Release --no-restore
dotnet test hot-desk-booking-system.sln -c Release --no-build --logger trx --collect:"XPlat Code Coverage" --results-directory tests/UnitTests/TestResults/dependency-upgrades
dotnet list hot-desk-booking-system.sln package --vulnerable --include-transitive --no-restore
dotnet list hot-desk-booking-system.sln package --deprecated --include-transitive --no-restore
docker build -f src/Web.Api/Dockerfile -t hotdesk-dependency-check:20261006 .
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-DependencySmoke.ps1
```

The smoke script uses the existing development seed account only inside its disposable database. It generates temporary database/JWT secrets and removes its containers afterward.

## Evidence and limits

- Local SDK: 10.0.400. Container build SDK: 10.0.401, permitted by the existing patch roll-forward policy.
- TRX: `tests/UnitTests/TestResults/dependency-upgrades/Janek_DESKTOP-JANEK_2026-10-06_17_33_57_net10.0.trx`.
- Coverage: `tests/UnitTests/TestResults/dependency-upgrades/cfd035de-96e4-40b0-9795-30ebfafb5e35/coverage.cobertura.xml`.
- Reports were inspected: 14 executed tests, no failures/skips, valid Cobertura output. Reports are local ignored build artifacts.
- Validation pipeline, password hasher, and JWT code have 100% line coverage. Changed mapping handler paths have 92-100%. Overall coverage is 9.98% (303/3034 lines, including migrations); this is focused regression coverage, not full application coverage.
- Existing nullable warnings in `GetDesksValidator.cs` remain for later work. MediatR's older-line maintenance tradeoff is recorded in [plan.md](plan.md).
