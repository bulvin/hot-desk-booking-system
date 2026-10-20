# Roadmap

## Phase 1: Migrate the solution to .NET 10 — complete

- [x] Retarget all production/test projects and configure the shared SDK.
- [x] Align framework and EF/Npgsql dependencies; fix migration-related build/test failures.

## Phase 2: Upgrade Docker and PostgreSQL — complete

- [x] Run .NET 10 containers with PostgreSQL 18 and its required data-directory layout.
- [x] Verify fresh startup, PostgreSQL 17-to-18 logical migration, retained records, and authenticated API requests.
- [x] Document local configuration and migration steps.

## Phase 3: Refresh application dependencies — complete

- [x] Upgrade validation, authentication, Swagger, and test packages; use source-generated Mediator and replace AutoMapper with explicit mapping.
- [x] Verify dispatch, validation, mappings, authentication, authorization, and HTTP contracts; record package/security/license decisions.

## Phase 4: Standardize formatting — complete

- [x] Add root `.editorconfig` for C# indentation, whitespace, line endings, and naming conventions shared by `src` and `tests`.
- [x] Apply a formatting-only change to handwritten code; preserve generated migrations.
- [x] Document and run `dotnet format hot-desk-booking-system.slnx --verify-no-changes` with the selected style diagnostics and migration exclusion.

## Phase 5: Configure static analysis — complete

- [x] Add root shared build settings with an explicit .NET 10 analyzer level and selected build-time style diagnostics.
- [x] Inventory actionable findings by rule, recording narrow exclusions for generated code.

## Phase 6: Resolve findings and enforce warnings — complete

- [x] Fix nullable/correctness findings in small batches, starting with nullable pagination access in `GetDesksValidator`.
- [x] Enable `TreatWarningsAsErrors` after the baseline is clean; explain each remaining suppression in `.editorconfig`.

## Phase 7: Normalize HTTP resource responses

- [x] Document endpoint error statuses and Problem Details schemas, including authentication, authorization, conflicts, and request-body errors.
- [x] Normalize empty HTTP errors through the Problem Details service and verify response metadata, RFC type links, and Swagger contracts.

## Phase 8: Keycloak authentication — complete

- [x] Delegate account lifecycle to Keycloak with OAuth 2.0/OpenID Connect and PKCE.
- [x] Validate and introspect access tokens; preserve booking ownership and role policies.
- [x] Configure Mailpit confirmation and Swagger authorization; remove local Identity services.

## Phase 9: Update location - complete

- [x] Update location (name, address) by administrator.

## Phase 10: Booking date consistency - complete

- [x] Use the injected UTC clock for booking validation and default desk search dates across every location.
- [x] Allow desk changes only before the reservation start day in UTC; keep reservation dates as inclusive calendar values and audit timestamps in UTC.
- [x] Add regression coverage for UTC midnight, inclusive booking limits, and desk-change cutoffs; document the UTC calendar in the API.

## Phase 11: Cancel reservation - complete

- [x] Owners cancel before the start day begins at midnight UTC; administrators cancel reserved bookings through their final UTC day. Completed bookings cannot be canceled.
- [x] Retain canceled history, free its dates, and make authorized repeats succeed without writes. Prevent changes to canceled reservations, including stale concurrent desk changes.
- [x] Return a conflict when deleting desks with reservation history; verify persistence and availability against PostgreSQL 18 and document the API contract.

## Phase 12: CI/CD

- [x] Add GitHub Actions CI for pull requests and pushes to the default branch, using the SDK from `global.json` on a Linux runner with Docker.
- [x] Run restore, Release build with warnings as errors, unit tests, PostgreSQL 18 integration tests through the existing Testcontainers fixture, and formatting verification with the agreed diagnostics and migration exclusion. Publish test results even on failure.
- [x] Add API Docker build verification and document the required CI status checks.
- [x] Add a release workflow to run checks on the tagged commit and publish the API image to GitHub Container Registry with version and commit tags; keep publishing credentials out of pull-request workflows.

## After the refresh

The React frontend, location browsing, and personal reservation listing are approved follow-up work (see below). Reservation retrieval, company-wide listing, multi-company tenancy, hourly/recurring bookings, notifications, reporting, and production deployment require separate scope decisions.

## Approved React frontend follow-up

The React frontend and minimal authenticated office/personal-reservation read APIs are now approved as separate scope. See [frontend.md](frontend.md) for workflows, local setup, and boundaries. Production deployment and company-wide reservation browsing remain deferred.
