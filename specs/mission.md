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
| Access an account | Administrator-created accounts with assigned passwords; local Identity login and JWT sessions; administrator/employee roles |
| Manage offices | Authenticated users browse locations; administrators create, edit, and delete them |
| Manage desks | Administrators create/delete desks and change availability; authenticated users list/filter desks by location and retrieve details |
| Reserve a desk | Authenticated users book an enabled desk for a date range; an application-level overlap check exists |
| Change a reservation | Owners can change the desk; target availability/overlap checks still need completion |
| Inspect reservations | Users list their own reservation history with desk and office details; desk details restrict employee identity to administrators; owners and administrators can cancel reservations; dedicated reservation retrieval and company-wide listing are absent |

## Agreed booking rules

- Start and end dates are inclusive. Equal dates mean a one-day booking; the maximum is seven days.
- All locations use the UTC calendar: today and booking cutoffs change at midnight UTC, regardless of server or client timezone. Reservation dates are calendar values and must not be shifted by clients as timestamps; audit timestamps remain UTC instants.
- Start dates cannot be in the past and may be at most seven days ahead. The end date can extend beyond that horizon within the seven-day duration limit.
- A desk must be enabled and free of overlapping active reservations for every requested day.
- Changing a desk must enforce ownership and the same availability rules as a new booking.
- Owners may change desks through the reservation start day in UTC; changes close at midnight starting the following day. The same cutoff applies when changing to a desk in another location.
- Owners may cancel through the reservation start day in UTC. For a Friday booking, cancellation is allowed throughout Friday and closes at 00:00 UTC on Saturday.
- Administrators may cancel reserved bookings through their final UTC day. Completed bookings cannot be canceled.
- Cancellation retains the reservation as `Canceled` and frees its entire date range. Authorized repeats succeed without further writes, including after the cutoff. Canceled and completed reservations cannot change desks.
- Desks with any reservation history cannot be deleted; deletion returns a conflict. Completed and canceled reservations do not block booking availability.
- Concurrent requests must not create overlapping active reservations for the same desk. A database safeguard is still required.
- Employees must not receive another employee's identity through reservation details; administrator access is intentional.

## Decisions required before related implementation

- Decide whether an employee may hold multiple desk reservations for the same day.

## Success criteria for the refresh

- The API, tests, and containers run on .NET 10 with PostgreSQL 18.
- Dependency choices are recorded and automated restore/build/test/format/analyzer checks pass.
- Desk search, desk changes, date boundaries, and concurrent booking have meaningful regression coverage.
- Database upgrades preserve records and migration history; local startup instructions are reproducible.
- HTTP responses and authorization behavior are consistent with the agreed workflows.

Keep the four-layer architecture. The React frontend and supporting read endpoints are approved follow-up work; hourly/recurring bookings, multi-company tenancy, notifications, reporting, production deployment, and additional endpoints remain deferred.

See [tech-stack.md](tech-stack.md) for implementation choices and [roadmap.md](roadmap.md) for delivery order.

## Approved React frontend follow-up

The React frontend and minimal authenticated office/personal-reservation read APIs are now approved as separate scope.

## Approved local account management

Replace Keycloak end to end with ASP.NET Core Identity and JWT sessions. Administrators create, list, deactivate, and reactivate employees. Administrator provisioning is handled separately; no bootstrap command is provided. Multiple administrator accounts are supported. No public registration, password setup/change/recovery, email confirmation, user deletion, or role editing is included. Preserve historical booking ownership and database migration history; no existing Keycloak account import is required. See the README for configuration and authentication contracts.
