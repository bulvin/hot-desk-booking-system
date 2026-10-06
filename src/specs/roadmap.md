# Refresh roadmap

Refresh the existing API on .NET 10 while retaining its architecture and 1-7-day booking model. See [mission.md](mission.md) and [tech-stack.md](tech-stack.md).

Each phase is a small feature slice with 1-3 work items and a verifiable outcome. All phases are planned. Include relevant tests and documentation in the slice that changes the behavior; keep mechanical formatting separate. Review dependency compatibility, maintenance, security, and licensing when touching each package.

## Phase 1: Run the solution on .NET 10

- [ ] Pin a reviewed .NET 10 SDK and retarget the API, libraries, and sibling test project.
- [ ] Align framework packages, stable EF Core/Npgsql 10 packages, and EF tooling; review applicable .NET 9/10 breaking changes.
- [ ] Verify restore, build, and existing tests; resolve migration-related failures.

Done when: the full solution builds and its tests run successfully on .NET 10 with a compatible dependency graph.

## Phase 2: Run the upgraded API with PostgreSQL in Docker

- [ ] Update .NET SDK/runtime container images and review the PostgreSQL image patch level.
- [ ] Verify migrations on a fresh database and a disposable copy containing existing data.
- [ ] Smoke-test container startup and an authenticated request; document local startup configuration.

Done when: the containerized API runs and migrations preserve existing data and migration history.

## Phase 3: Refresh request validation

- [ ] Update compatible FluentValidation core/DI packages and remove unused unsupported ASP.NET integration.
- [ ] Verify the MediatR validation pipeline and validation error responses.

Done when: valid requests reach their handlers and invalid requests return the expected validation errors.

## Phase 4: Refresh request dispatch

- [ ] Record and implement the MediatR keep/update/replace decision after compatibility and license review.
- [ ] Verify command/query dispatch and validation behavior ordering.

Done when: request dispatch works with the selected supported dependency arrangement.

## Phase 5: Refresh DTO mapping

- [ ] Record and implement the AutoMapper keep/update/replace decision after compatibility and license review.
- [ ] Verify the existing DTO mappings and response shapes.

Done when: mapping is verified and any upgrade or replacement preserves API contracts.

## Phase 6: Refresh API documentation

- [ ] Align retained OpenAPI/Swashbuckle packages and remove unused references.
- [ ] Verify documented routes, schemas, and bearer authentication in Swagger.

Done when: Swagger loads and accurately describes the existing API.

## Phase 7: Refresh authentication dependencies

- [ ] Review and update BCrypt and remaining authentication dependencies where justified.
- [ ] Verify password hashing, login, JWT validation, and employee/admin access boundaries.

Done when: authentication and role enforcement pass focused regression checks.

## Phase 8: Refresh test tooling

- [ ] Review and update the test SDK, xUnit runner, Moq, and coverage collector.
- [ ] Repair stale test expectations and verify test discovery and coverage collection.

Done when: the full test suite executes successfully and produces usable results and coverage.

## Phase 9: Standardize formatting

- [ ] Add a shared `.editorconfig` covering production and test code.
- [ ] Apply mechanical formatting in a dedicated change.
- [ ] Verify formatting with `dotnet format --verify-no-changes`.

Done when: the solution passes repeatable formatting checks.

## Phase 10: Enable static analysis

- [ ] Configure SDK analyzers, an explicit analysis level, and useful build-time style diagnostics.
- [ ] Resolve compiler/nullability findings and analyzer findings in small category-based batches.
- [ ] Evaluate extra analyzer packages only for demonstrated gaps.

Done when: the agreed analyzer rules cover production and tests, with findings fixed or narrowly justified.

Shared build configuration must cover both `src` and sibling `tests/UnitTests`, through the common repository ancestor or explicit imports.

## Phase 11: Enforce code quality in CI

- [ ] Add CI restore/build/test checks using the pinned SDK and publish test results.
- [ ] Enforce formatting and the reviewed analyzer/warnings-as-errors policy.
- [ ] Verify that deliberate check violations fail CI.

Done when: a clean checkout passes the same quality gates locally and in CI.

## Phase 12: Make booking dates consistent

- [ ] Define the business timezone, booking horizon, and desk-change cutoff.
- [ ] Use a controlled clock consistently in date-dependent behavior.
- [ ] Cover inclusive 1-7-day ranges, past dates, and cutoff boundaries with regression tests.

Done when: date rules are explicit and deterministic without changing the agreed booking model.

## Phase 13: Make desk search reliable

- [ ] Validate null filters, positive bounded pagination, and date ranges in the `GetDesksHandler` flow.
- [ ] Correct bookability predicates to account explicitly for reservation status and agreed disabled-desk semantics.
- [ ] Verify results with PostgreSQL-backed filtering tests.

Done when: desk searches return the correct available desks for the requested inclusive range.

## Phase 14: Make desk changes safe

- [ ] Validate target availability and overlapping active reservations.
- [ ] Enforce ownership, reservation status, and the agreed cutoff.
- [ ] Test accepted and rejected desk changes.

Done when: changing desks preserves the same availability guarantees as initial booking.

## Phase 15: Prevent concurrent double booking

- [ ] Add a PostgreSQL-backed overlap safeguard covering booking and desk changes.
- [ ] Map rejected conflicts to a consistent API response.
- [ ] Run simultaneous conflicting-request integration tests against disposable PostgreSQL in CI.

Done when: conflicting requests cannot both create overlapping active reservations for a desk.

## Phase 16: Make reservation-related responses consistent

- [ ] Verify reservation privacy, Problem Details responses, and Created resource URLs.
- [ ] Define and test how reservation status and history affect desk/location deletion.

Done when: these API contracts are predictable and historical reservations do not cause unhandled database failures.

## Phase 17: Automate ongoing maintenance

- [ ] Enable periodic dependency update proposals and vulnerability/deprecation review.
- [ ] Add migration and desk-filter integration checks to CI alongside concurrency tests.
- [ ] Update setup instructions and dependency decisions; verify startup from a clean checkout.

Done when: maintenance checks are repeatable and a returning developer can run and verify the refreshed project.

## Completion criteria

The .NET 10 API and container run, dependency decisions are recorded, migrations preserve data, and build/test/format/analyzer checks pass in CI. Critical booking guarantees have regression coverage.

Use disposable databases for verification and preserve existing data by default. New endpoints, multi-company tenancy, and production deployment remain future work.
