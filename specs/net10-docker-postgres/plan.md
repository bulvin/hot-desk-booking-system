# Plan: Run .NET 10 with PostgreSQL in Docker

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
