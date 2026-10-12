# Roadmap

## Phase 1: Migrate the solution to .NET 10 — complete

- [x] Retarget all production/test projects and configure the shared SDK.
- [x] Align framework and EF/Npgsql dependencies; fix migration-related build/test failures.

Done when: solution restore, Release build, and existing tests pass.

## Phase 2: Upgrade Docker and PostgreSQL — complete

- [x] Run .NET 10 containers with PostgreSQL 18 and its required data-directory layout.
- [x] Verify fresh startup, PostgreSQL 17-to-18 logical migration, retained records, and authenticated API requests.
- [x] Document local configuration and migration steps.

Done when: container startup and migration checks pass without losing data/history.

## Phase 3: Refresh application dependencies — complete

- [x] Upgrade validation, authentication, Swagger, and test packages; use source-generated Mediator and replace AutoMapper with explicit mapping.
- [x] Verify dispatch, validation, mappings, authentication, authorization, and HTTP contracts; record package/security/license decisions.

Done when: reviewed dependency graph, Release build, 14 tests, and HTTP smoke checks pass.

## Phase 4: Standardize formatting — complete

- [x] Add root `.editorconfig` for C# indentation, whitespace, line endings, and naming conventions shared by `src` and `tests`.
- [x] Apply a formatting-only change to handwritten code; preserve generated migrations.
- [x] Document and run `dotnet format hot-desk-booking-system.slnx --verify-no-changes` with the selected style diagnostics and migration exclusion.

Done when: the formatting check passes and the diff contains no behavior changes.

## Phase 5: Configure static analysis — complete

- [x] Add root shared build settings with an explicit .NET 10 analyzer level and selected build-time style diagnostics.
- [x] Inventory actionable findings by rule, recording narrow exclusions for generated code.

Done when: Release builds report the same selected diagnostics for production and tests; any existing findings are explicitly listed for Phase 6.

## Phase 6: Resolve findings and enforce warnings — complete

- [x] Fix nullable/correctness findings in small batches, starting with nullable pagination access in `GetDesksValidator`.
- [x] Enable `TreatWarningsAsErrors` after the baseline is clean; explain each remaining suppression in `.editorconfig`.

Done when: Release build and tests pass with the selected warnings enforced and no blanket suppressions hiding findings.

## Phase 7: Normalize HTTP resource responses

- [x] Document endpoint error statuses and Problem Details schemas, including authentication, authorization, conflicts, and request-body errors.
- [x] Normalize empty HTTP errors through the Problem Details service and verify response metadata, RFC type links, and Swagger contracts.

## Phase 8: Keycloak authentication — complete

- [x] Delegate account lifecycle to Keycloak with OAuth 2.0/OpenID Connect and PKCE.
- [x] Validate and introspect access tokens; preserve booking ownership and role policies.
- [x] Configure Mailpit confirmation and Swagger authorization; remove local Identity services.

Done when: hosted registration/login, confirmed email, account management, password changes, current-session logout and booking authorization pass the [feature validation](2026-10-10-identity-authentication/validation.md).

## Phase 9: Add CI quality gates

- [ ] Run restore, Release build, tests, and formatting checks using the repository SDK policy.
- [ ] Publish TRX/coverage artifacts and verify that a deliberate formatting or analyzer violation fails CI.

Done when: a clean checkout passes the documented local commands and the same checks in CI.

## After the refresh

Plan new endpoints separately: location browsing/editing and reservation listing/retrieval/cancellation. Frontend, multi-company tenancy, hourly/recurring bookings, notifications, reporting, and production deployment require separate scope decisions.
