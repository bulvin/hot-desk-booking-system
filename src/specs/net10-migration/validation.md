# Validation: .NET 10 migration

Status: Phase 1 checks passed on 2026-10-06. Existing warnings and runtime verification limits are recorded below.

## 1. SDK and target checks

- [x] Repository-root `global.json` selects the agreed stable .NET 10 SDK with the recorded roll-forward policy.
- [x] `dotnet --version` resolves the intended SDK from both the repository root and `tests/UnitTests`.
- [x] All five production/test project files target `net10.0`.
- [x] Resolved framework and EF/Npgsql dependencies are compatible; no prerelease persistence/tooling references or unresolved dependency conflict/downgrade warnings remain.

## 2. Required build and test checks

Run from the repository root after the final implementation changes:

```powershell
dotnet --version
dotnet restore src/hot-desk-booking-system.sln
dotnet build src/hot-desk-booking-system.sln --configuration Release --no-restore --disable-build-servers -m:1
dotnet test src/hot-desk-booking-system.sln --configuration Release --no-build --no-restore --logger trx --disable-build-servers -m:1
dotnet list src/hot-desk-booking-system.sln package --include-transitive --no-restore
git diff --check
```

- [x] Restore, Release build, and test commands exit successfully.
- [x] The expected existing tests are discovered and all pass; zero discovered tests is a failure, not a successful check.
- [x] No tests were removed, newly skipped, or weakened to hide failures.
- [x] Each corrected stale assertion is justified against intended behavior and source evidence.
- [x] Warnings introduced by the migration are resolved or explicitly reviewed and justified. Existing unrelated warnings are recorded for later quality work; this slice does not enable a blanket warnings-as-errors policy.

## 3. Scope and compatibility review

- [x] Changes retain the layered architecture, HTTP contracts, and inclusive 1-7-day booking behavior.
- [x] Database schema, migration history, and data are untouched.
- [x] Deferred dependency changes appear only where needed for compatibility, with a recorded reason.
- [x] Unrelated working-tree changes are excluded from migration commits unless separately authorized.
- [x] Dependency decisions and verification evidence are recorded, and the final diff has no whitespace errors.

## 4. Evidence to record during implementation

| Check | Result / evidence |
| --- | --- |
| Selected and resolved SDK | PASS: 10.0.400 from repository root and tests/UnitTests |
| Target frameworks and resolved dependency graph | PASS: all five projects use net10.0; EF runtime/relational/design/tools resolve to 10.0.12 where used; Npgsql provider/driver 10.0.3; no remaining EF conflicts |
| Restore | PASS: exit 0; pre-existing NU1903 AutoMapper advisory remains |
| Release build and warning review | PASS: exit 0, zero errors; final incremental build reports four NU1903 warnings (same dependency across four projects). Two pre-existing CS8602 warnings appeared during compilation of Application. |
| Tests: discovered / passed / failed / skipped | PASS: 3 / 3 / 0 / 0 |
| TRX results location | tests/UnitTests/TestResults/Janek_DESKTOP-JANEK_2026-10-06_13_08_36_net10.0.trx (local ignored build artifact) |
| Corrected tests and rationale | PASS: missing-desk test now asserts existing DeskNotFoundException, ID, HTTP 404, and message; the two employee/admin reservation-detail cases remain passing |
| Final diff and scope review | PASS: no production C# or migration files changed; git diff --check passes. Existing solution-file edit preserved and excluded from the migration commit. |

### Execution notes

The initial .NET 8 baseline compiled production projects but failed writing sibling test outputs, so a complete pre-migration test run was unavailable. The first migrated test run discovered three cases: two passed and one exposed the stale exception assertion described above.

Some reused MSBuild processes retained restricted access to test output/cache files. Final verification succeeded with `--disable-build-servers -m:1` and authorized access to the sibling tests directory. This was an execution-environment issue, not a project workaround.

AutoMapper's existing high-severity advisory is not suppressed or fixed by this slice. Track remediation in Phase 5 before treating the application as production-ready. See the dependency decisions in [requirements.md](requirements.md).

## Merge gate

The implementation can be merged once all required checks above pass and their results are recorded. A restore failure, build failure, undiscovered/failing test, incompatible dependency graph, or unexplained behavioral change blocks completion of Phase 1.

Passing these checks establishes build/test compatibility on .NET 10. It does not establish PostgreSQL migration/runtime or container compatibility: those checks remain in roadmap Phase 2, and Docker images remain on their existing version until that phase.
