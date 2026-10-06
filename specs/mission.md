# Mission

## Purpose

Build a RESTful API for one company to manage shared desks across one or more office locations. Employees find a desk and reserve it for an inclusive range of 1-7 whole calendar days.

The current priority is to refresh and stabilize the existing API: maintain supported dependencies, make code quality checks repeatable, and close gaps in booking correctness before adding endpoints.

## Target audience

- Companies operating shared desks, including hybrid teams, at one or multiple offices.
- Office managers acting as administrators who create locations and desks and control desk availability.
- Employees who need to find and reserve desks for upcoming office days.
- Internal developers building clients against the API.

Each deployment serves one company. Multi-company tenancy is outside the agreed scope.

## Core workflows and current coverage

| Workflow | Current implementation |
| --- | --- |
| Access an account | Employee registration and login; JWT authentication; administrator/employee roles |
| Manage offices | Administrators create and delete locations; location browsing and editing endpoints are absent |
| Manage desks | Administrators create/delete desks and change availability; authenticated users list/filter desks by location and retrieve details |
| Reserve a desk | Authenticated users book an enabled desk for a date range; an application-level overlap check exists |
| Change a reservation | Owners can change the desk; target availability/overlap checks still need completion |
| Inspect reservations | Desk details expose reservation information with employee identity restricted to administrators; dedicated reservation listing/get/cancel endpoints are absent |

## Agreed booking rules

- Start and end dates are inclusive. Equal dates mean a one-day booking; the maximum is seven days.
- Start dates cannot be in the past and may be at most seven days ahead. The end date can extend beyond that horizon within the seven-day duration limit.
- A desk must be enabled and free of overlapping active reservations for every requested day.
- Changing a desk must enforce ownership and the same availability rules as a new booking.
- Concurrent requests must not create overlapping active reservations for the same desk. A database safeguard is still required.
- Employees must not receive another employee's identity through reservation details; administrator access is intentional.

## Decisions required before related implementation

- Define the business timezone used to determine today and booking cutoffs.
- Clarify the desk-change cutoff: the current code compares dates after adding 24 hours to the current server time.
- Decide whether an employee may hold multiple desk reservations for the same day.
- Define how completed/canceled reservations affect availability and deletion of desks with history.

## Success criteria for the refresh

- The API, tests, and containers run on .NET 10 with PostgreSQL 18.
- Dependency choices are recorded and automated restore/build/test/format/analyzer checks pass.
- Desk search, desk changes, date boundaries, and concurrent booking have meaningful regression coverage.
- Database upgrades preserve records and migration history; local startup instructions are reproducible.
- HTTP responses and authorization behavior are consistent with the agreed workflows.

Keep the four-layer architecture during the refresh. Frontend development, hourly/recurring bookings, multi-company tenancy, notifications, reporting, production deployment, and new endpoints are deferred.

See [tech-stack.md](tech-stack.md) for implementation choices and [roadmap.md](roadmap.md) for delivery order.
