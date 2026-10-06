# Mission

## Purpose

Provide a RESTful API that lets one company manage desks across one or more office locations and lets its employees reserve available desks for whole calendar days.

The immediate goal is to make the existing project easy to resume, maintain, test, and upgrade. Refresh the platform and development practices before expanding the product.

## Agreed scope

- One company per deployment, with multiple locations; tenant isolation is outside the current scope.
- Reservations cover an inclusive range of 1–7 whole days. A single-day booking has the same start and end date.
- Keep the existing layered architecture and review dependencies individually.
- Prioritize migration from .NET 8 to .NET 10, dependency maintenance, static analysis, tests, and repeatable development checks.

## Target audience

- Companies with shared desks in one or more office locations, including teams working in a hybrid model. Each deployment serves a single company.
- Office managers and administrators who need to manage locations, desks, and desk availability centrally.
- Employees who need to find an available desk and reserve it for 1–7 whole days, or change their reserved desk subject to booking rules.
- Internal development teams integrating desk booking into company applications through the RESTful API.

## Existing functionality

Source review on 2026-10-06 found the following implementation. This inventory does not imply that it has passed runtime verification.

| Area | Present in source |
| --- | --- |
| Accounts | Employee registration, password hashing, login, JWT issuance, role policies |
| Locations | Administrator creation and deletion |
| Desks | Creation, deletion, availability changes, details, paginated listing by location |
| Search | Availability and date-range booking filters |
| Reservations | Booking a desk and changing the desk on an owned reservation |
| API support | Swagger, validation pipeline, exception handling with Problem Details |

Location browsing and reservation listing, retrieval, and cancellation endpoints are not currently implemented. Reservation status values exist, but a complete status lifecycle is not exposed.

## Booking behavior to preserve and verify

- Reject past start dates, reversed ranges, and ranges longer than seven inclusive days.
- The current validator permits a start date up to seven days ahead. It does not require the end date to fall within that same horizon; preserve this distinction during the platform migration.
- A desk must be enabled and free of overlapping active reservations throughout the requested range.
- Desk changes require ownership and must obey the same availability and overlap guarantees as initial booking.
- Concurrent requests must not create overlapping active reservations for the same desk.

The existing desk-change cutoff compares dates after adding 24 hours to the current time. Clarify the intended calendar-day cutoff before changing that rule. Also define the company's business timezone and whether an employee may reserve multiple desks on the same day before adding new restrictions.

## Refresh success criteria

- The API, all libraries, tests, and container build target .NET 10 consistently.
- Dependencies have recorded compatibility, maintenance, security, and license decisions.
- Formatting, static analysis, build, and tests are repeatable locally and enforced in CI.
- Existing booking behavior has useful regression coverage; known correctness gaps are tracked separately from platform changes.
- A returning developer can configure secrets, start PostgreSQL and the API, and run checks from documented instructions.

## Deferred scope

Multi-company tenancy, frontend development, hourly or recurring reservations, notifications, reporting, and production hosting are outside this refresh. New endpoints can follow as a separate roadmap after the maintenance baseline is healthy.

See [tech-stack.md](tech-stack.md) for the current and intended stack and [roadmap.md](roadmap.md) for the implementation order.
