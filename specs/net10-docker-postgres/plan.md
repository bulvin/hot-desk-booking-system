# Plan: Run .NET 10 with PostgreSQL in Docker

Status: implemented and validated on 2026-10-06.

Roadmap: [Phase 2](../roadmap.md#phase-2-run-the-upgraded-api-with-postgresql-in-docker).
Branch: `chore/net10-docker-postgres`.
Base: `chore/net10-migration` at `01b22e0`.

## Scope and decisions

Make the migrated API build and run in Docker with PostgreSQL, verify existing migrations and retained data, and document reproducible local startup. Keep the existing architecture and booking behavior.

At the start of this phase, the Dockerfile used .NET 8 images despite the projects targeting .NET 10. Compose built from `src`, excluding the repository-root `global.json`. PostgreSQL used `postgres:17`; startup applies migrations and seeds data. Compose had fixed container names, host ports, and a local database bind mount.

Upgrade PostgreSQL from 17 to 18 using the major-version image tag. Align the container SDK with the repository SDK policy. Use .NET 10.0 image tags and permit newer SDK feature bands within 10.0.

Use isolated disposable databases for verification. Do not modify or reset the developer's existing database. A different Compose project name alone is insufficient isolation with the current fixed names, ports, and bind mount; verification configuration must isolate all of them. Use a logical database copy or a representative pre-upgrade fixture, not a live data-directory copy.

Independent dependency upgrades, static analysis, new endpoints, and production deployment remain later work. Fix runtime compatibility failures required for this slice and record any necessary scope exception. Preserve the existing solution-file edit.

## 1. Align container builds with .NET 10

- [x] Select compatible .NET SDK/runtime image versions and use the PostgreSQL 18 image tag; record image choices and rationale here.
- [x] Adjust Dockerfile/build context and `.dockerignore` as needed so container builds honor the root SDK policy and copy required projects without including secrets or local database files. Update the PostgreSQL volume mount for version 18: mount at `/var/lib/postgresql`, with default `PGDATA=/var/lib/postgresql/18/docker`; use a separate destination from the existing 17 data directory.
- [x] Build the API image from a clean context and confirm its SDK/runtime versions, non-root execution, and expected port configuration.

Done when: the image restores and publishes the .NET 10 API successfully and its build inputs match the repository's SDK policy.

## 2. Verify migrations and retained data

- [x] Prepare isolated PostgreSQL verification environments and apply the current migrations through API startup to a fresh database; confirm roles and initial user seeding.
- [x] Use PostgreSQL 18 dump/restore tooling to migrate a logical export from PostgreSQL 17 into a separate PostgreSQL 18 database, using existing data or a documented pre-upgrade fixture; run the upgraded API and compare migration history and representative users, roles, locations, desks, and reservations before and after.
- [x] Restart the API and database to verify persistence and repeatable startup without duplicate seeds or unexpected migration/model changes; fix only compatibility issues demonstrated by these checks.

Done when: fresh and retained-data scenarios both start successfully, existing records and relationships survive, migration history is preserved, and repeated startup succeeds. If only a fixture is available, record that limitation explicitly.

## 3. Verify the running API and document startup

- [x] Exercise HTTP requests against the container: Swagger generation, registration/login, an authenticated desk-list request, and an unauthenticated rejection. Use a known location and synthetic data in the isolated database; record expected and actual status codes and response shapes.
- [x] Document local Docker prerequisites, required configuration keys, secret setup, database hostname inside Compose, the 17-to-18 dump/restore procedure and rollback to the preserved 17 database, startup/restart commands, and safe cleanup limited to disposable verification resources. Never include secret values in tracked documentation or evidence.
- [x] Re-run Release build and existing tests, record container/database/HTTP results here, and mark roadmap Phase 2 complete only after the checks pass.

Done when: a developer can reproduce container startup, authentication and an actual database-backed API request work without assembly/version errors, and regression tests remain passing.

## Evidence and completion checklist

| Check | Result |
| --- | --- |
| Selected SDK/runtime/PostgreSQL images and rationale | Tags sdk:10.0, aspnet:10.0, and postgres:18 follow the requested version policy. Original runtime verification used SDK 10.0.400, runtime 10.0.11, and PostgreSQL 18.6; those numbers record evidence, not version pins. |
| Clean container build and effective SDK/runtime versions | PASS: no-cache build; SDK 10.0.400; runtime 10.0.11; uid/gid 1654; HTTP port 8080; filtered build context 246.88 kB. |
| Fresh database migration and seeding | PASS: PostgreSQL 18.6 initialized through startup, six migrations, seeded roles/admin; no model or assembly error. |
| PostgreSQL 17-to-18 logical migration and before/after comparison | PASS: synthetic fixture created through old .NET 8 API on PostgreSQL 17; PostgreSQL 18 pg_dump/pg_restore into separate database; counts and row-content hashes match across all seven tables. |
| Repeated startup and persistence | PASS: PostgreSQL 18 and both new APIs restarted; all seven table hashes unchanged; Swagger still responds. |
| Swagger and HTTP authentication/database smoke checks | PASS: Swagger/login/registration/list 200, location/desk creation 201, unauthenticated list 401. Migrated employee login and retained reservation ID/dates work; employee response hides reserver identity. |
| Release build and existing tests | PASS: Release build and 3/3 tests; zero failures/skips. TRX: tests/UnitTests/TestResults/Janek_DESKTOP-JANEK_2026-10-06_13_54_30_net10.0.trx. Existing AutoMapper/nullable warnings remain. |
| Documented startup reproduced | PASS: Compose configuration validated and started with generated test credentials, an isolated volume, and alternate port; Swagger 200. See ../../docs/docker-development.md for local setup and migration commands. |

Completion requires all checks above to pass with evidence. A successful image build alone is insufficient. Keep this work based on the migration branch; merge the combined refresh into `main` only when explicitly requested. This phase does not resolve the separately tracked AutoMapper vulnerability or establish full production readiness.

## Upgrade references

- [PostgreSQL 18 release and migration notes](https://www.postgresql.org/docs/release/18.0/).
- [Official PostgreSQL Docker image and version 18 storage layout](https://hub.docker.com/_/postgres).

Do not start PostgreSQL 18 on a PostgreSQL 17 data directory. The major-version data upgrade is separate from EF schema migrations; verify both. Keep the original 17 database intact until the restored 18 database has passed verification.

## Implementation evidence and limits

- Docker build context now includes the root SDK policy through a Dockerfile-specific allowlist. Local database/secrets directories are excluded.
- Compose retains project hot_desk_booking and image web.api, generated service names, loopback host ports, and separate .containers/db18 storage mounted at /var/lib/postgresql.
- Fixture contained 1 desk, 1 location, 1 reservation, 2 roles, 2 user-role links, 2 users, and 6 migration-history records. Compared full row-content hashes before/after restore and restart, not just counts.
- Verification used synthetic data, not a copy of company data. The original web.api/postgres containers and PostgreSQL 17 bind mount were not modified.
- No production C# or schema changes were required. The existing development seeder, AutoMapper advisory, and ephemeral Data Protection key warnings remain follow-ups; no production-readiness claim is made.
- [Startup and 17-to-18 migration guide](../../docs/docker-development.md).
