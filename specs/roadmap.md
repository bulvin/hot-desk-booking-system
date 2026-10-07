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

- [x] Upgrade validation, authentication, Swagger, and test packages; retain reviewed MediatR 12 and replace AutoMapper with explicit mapping.
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

Validation: Release build has zero warnings/errors; all 21 tests pass, including optional pagination, cancellation forwarding, JWT issuer/audience checks, and EF model compatibility. A deliberate compiler warning fails the build. Existing migrations are unchanged. JWT issuer/audience now default to `hot-desk-booking-system` / `hot-desk-booking-system-api`; existing tokens require a new sign-in.

## Phase 7: Add CI quality gates

- [ ] Run restore, Release build, tests, and formatting checks using the repository SDK policy.
- [ ] Publish TRX/coverage artifacts and verify that a deliberate formatting or analyzer violation fails CI.

Done when: a clean checkout passes the documented local commands and the same checks in CI.

## Phase 8: Define and centralize booking dates

- [ ] Decide business timezone and desk-change cutoff; document start-date horizon and inclusive 1-7-day duration.
- [ ] Use one controllable time source in booking validators, desk-list defaults, and desk-change checks.
- [ ] Test midnight/date boundaries, past dates, maximum duration, and cutoff equality.

Done when: date-dependent behavior is deterministic and matches the documented decisions.

## Phase 9: Validate desk-search inputs

- [ ] Define and enforce null/default filters, page >= 1, page size 1-30, and valid start/end combinations.
- [ ] Test GetDesksHandler defaults and invalid requests through the validation pipeline.

Done when: omitted filters work and invalid pagination/date ranges return validation errors rather than exceptions or misleading results.

## Phase 10: Correct desk availability queries

- [ ] Define enabled versus bookable filter semantics and make reservation status explicit in overlap predicates.
- [ ] Add PostgreSQL-backed cases for inclusive overlap boundaries, disabled desks, and canceled/completed reservations.

Done when: listing results and counts consistently reflect availability across the requested range.

## Phase 11: Enforce desk-change rules

- [ ] Check ownership, reservation status, the agreed cutoff, and target desk availability/overlap before updating.
- [ ] Cover accepted changes and each rejection path with regression tests.

Done when: moving a reservation obeys the same availability guarantees as creating one.

## Phase 12: Prevent concurrent overlapping bookings

- [ ] Add a PostgreSQL safeguard for overlapping active reservations covering both creation and desk changes.
- [ ] Translate database conflicts to a consistent HTTP conflict response.
- [ ] Test simultaneous requests against disposable PostgreSQL, including a booking racing with a desk change.

Done when: conflicting requests cannot both succeed and rejected operations leave valid data.

## Phase 13: Normalize HTTP resource responses

- [ ] Review Created URLs, existing resource routes, Problem Details, and reservation identity visibility.
- [ ] Add focused HTTP tests for success/error statuses and employee/admin response differences.

Done when: existing endpoints follow documented response contracts and Created URLs point to implemented routes.

## Phase 14: Preserve reservation history during deletion

- [ ] Decide how active/historical reservations affect desk deletion and how contained desks affect location deletion.
- [ ] Implement the agreed behavior and test foreign-key constraints and error responses with PostgreSQL.

Done when: deletion has predictable outcomes and does not lose history or expose unhandled database errors.

## Phase 15: Automate database regression checks

- [ ] Run migration, availability-query, and concurrency checks in CI with isolated PostgreSQL 18.
- [ ] Publish failure evidence and clean up only the resources created by the checks.

Done when: CI detects database regressions without depending on a developer's database.

## Phase 16: Automate dependency maintenance

- [ ] Configure scheduled dependency update proposals and vulnerability/deprecation checks, with a documented review policy.
- [ ] Reproduce the documented local startup and verification steps from a clean checkout.

Done when: dependency review is repeatable and setup instructions match the actual application.

## After the refresh

Plan new endpoints separately: location browsing/editing and reservation listing/retrieval/cancellation. Frontend, multi-company tenancy, hourly/recurring bookings, notifications, reporting, and production deployment require separate scope decisions.
