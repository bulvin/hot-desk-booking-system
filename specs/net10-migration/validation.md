# Validation: .NET 10 migration

## 1. SDK and target checks

- [x] Repository-root `global.json` selects the agreed stable .NET 10 SDK with the recorded roll-forward policy.
- [x] `dotnet --version` resolves the intended SDK from both the repository root and `tests/UnitTests`.
- [x] All five production/test project files target `net10.0`.
- [x] Resolved framework and EF/Npgsql dependencies are compatible; no prerelease persistence/tooling references or unresolved dependency conflict/downgrade warnings remain.

## 2. Required build and test checks

Run from the repository root after the final implementation changes:

```powershell
dotnet --version
dotnet restore hot-desk-booking-system.slnx
dotnet build hot-desk-booking-system.slnx --configuration Release --no-restore --disable-build-servers -m:1
dotnet test hot-desk-booking-system.slnx --configuration Release --no-build --no-restore --logger trx --disable-build-servers -m:1
dotnet list hot-desk-booking-system.slnx package --include-transitive --no-restore
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
