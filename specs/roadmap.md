# Roadmap

## Phase 1: Run the solution on .NET 10

- [x] Move the application and tests to .NET 10 and choose a shared SDK version.
- [x] Update the main framework and database libraries to compatible stable versions.
- [x] Build the solution, run existing tests, and fix problems caused by the upgrade.

Done when: the full solution builds and existing tests pass on .NET 10.

## Phase 2: Run the upgraded API and database in Docker

- [x] Update the .NET containers and upgrade PostgreSQL from 17 to 18.
- [x] Check database setup and transfer existing data using disposable test databases.
- [x] Check that the API starts, accepts a request from a signed-in user, and has documented setup steps.

Done when: the API runs in Docker and database upgrades preserve existing data and the record of earlier database changes.

## Phase 3: Upgrade dependencies for .NET 10

- [ ] Review and upgrade the supporting libraries for request checks, request handling, data conversion, API documentation, login, and testing.
- [ ] Check compatibility, support, security, and licensing. Keep or replace libraries where appropriate, remove unused packages, and record the decisions.
- [ ] Verify that requests, responses, login, access permissions, and Swagger still work as expected. Run the full test suite and check its reports.

Done when: the supporting libraries work with .NET 10, their review decisions are recorded, and the application and tests pass the migration checks.

## Phase 4: Make formatting consistent

- [ ] Define shared formatting rules for application and test code.
- [ ] Apply formatting in a separate change.
- [ ] Add a repeatable check for formatting.

Done when: the whole solution follows the same formatting rules and passes the check.

## Phase 5: Add automated code checks

- [ ] Choose useful checks that catch potential mistakes and unclear code before the application runs.
- [ ] Fix the findings in small groups and explain any exceptions.
- [ ] Add extra checking tools only when they solve a clear problem.

Done when: the agreed checks cover application and test code, with findings fixed or explained.

Shared settings must cover both the application in `src` and the tests in `tests/UnitTests`.

## Phase 6: Run quality checks automatically

- [ ] Set up CI: automated checks that build the application and run tests when code changes are submitted.
- [ ] Include formatting and the agreed code checks, and publish test results.
- [ ] Confirm that a change breaking these rules fails the automated checks.

Done when: a fresh copy of the project passes the same checks locally and in CI.

## Phase 7: Make booking dates consistent

- [ ] Agree on the company's timezone, how far ahead people can book, and the deadline for changing desks.
- [ ] Use a consistent source of time that can also be controlled in tests.
- [ ] Test one-to-seven-day bookings, past dates, and the exact points where deadlines apply.

Done when: date rules are clear and tests give repeatable results while preserving the agreed booking model.

## Phase 8: Make desk search reliable

- [ ] Check search filters, date ranges, and limits on the number of results returned at once.
- [ ] Make sure search respects reservation status and the agreed rules for disabled desks.
- [ ] Test search results using PostgreSQL.

Done when: searches return the correct available desks for every day in the requested range.

## Phase 9: Make desk changes safe

- [ ] Check that the replacement desk is available and has no overlapping active bookings.
- [ ] Check reservation ownership, status, and the agreed deadline for changes.
- [ ] Test changes that should succeed and changes that should be rejected.

Done when: changing desks follows the same availability rules as making a booking.

## Phase 10: Prevent double booking

- [ ] Add a database safeguard that prevents overlapping active bookings, including when someone changes desks.
- [ ] Return a clear, consistent response when a booking conflicts with another reservation.
- [ ] Automatically test people trying to book the same desk at the same time, using a disposable database.

Done when: two conflicting requests cannot both reserve the same desk for overlapping dates.

## Phase 11: Make reservation responses predictable

- [ ] Check reservation privacy, clear error responses, and links returned for newly created reservations.
- [ ] Define and test what happens when a desk or location with reservation history is deleted.

Done when: responses are consistent and reservation history does not cause unexpected database errors.

## Phase 12: Make ongoing maintenance easier

- [ ] Set up regular library update suggestions and checks for security issues or unsupported packages.
- [ ] Add automated database-upgrade and desk-search tests alongside the double-booking tests.
- [ ] Update setup instructions and library decisions, then check that a fresh copy of the project starts successfully.

Done when: maintenance checks are repeatable and a returning developer can start and verify the project from the documentation.
