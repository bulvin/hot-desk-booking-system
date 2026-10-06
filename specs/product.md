# Product

## What it does

This system helps a company manage shared office desks. Employees use it to find an available desk and reserve it for one to seven whole days.

It provides an API: a service that other company applications can connect to. A user-facing website is outside the current scope.

## Who it is for

- Employees who need a desk when they come to the office.
- Office managers who manage locations, desks, and availability.
- Developers who connect desk booking to company applications.

Each installation serves one company and can support multiple office locations.

## What is already available

The source code includes account registration and login, administrator controls, location and desk management, availability search, desk booking, and changing the desk on an existing reservation.

It also includes API documentation and checks for invalid requests. This list comes from a source review on 2026-10-06; the features have not been verified by running the application as part of this documentation work.

Browsing locations and viewing or cancelling reservations through dedicated API actions are not implemented yet. The full process for changing reservation status is also unfinished.

## Booking rules

- Bookings cover one to seven whole calendar days, counting both the start and end date.
- A booking cannot start in the past. Its end date cannot be before its start date.
- The current rules allow a start date up to seven days ahead. The end date can extend beyond that window, as long as the booking stays within seven days in total.
- A desk must be enabled and available for the entire booking.
- Employees can change only their own reservations, and the replacement desk must meet the same availability rules.
- Two people must not be able to reserve the same desk for overlapping dates, even if they book at the same time. This needs verification.

Before changing booking behavior, clarify the deadline for changing desks, the company's timezone, and whether one employee can book multiple desks for the same day. The current desk-change deadline uses the date obtained by adding 24 hours to the current time.

## What we are improving now

Make the existing project easy to resume, maintain, and test before adding features:

- Upgrade the application, tests, and containers from .NET 8 to .NET 10.
- Review supporting packages for compatibility, support, security, and licensing.
- Make formatting, code checks, and tests repeatable locally and automated when code changes are submitted.
- Add useful tests for existing booking rules and track known problems separately from the platform upgrade.
- Document how a returning developer can configure secrets, start the system, and run checks.

Keep the existing code structure during this work. See [tech-stack.md](tech-stack.md) for a plain-language explanation of the technologies and [roadmap.md](roadmap.md) for the work order.

## Later work

Support for multiple companies in one installation, a user-facing website, hourly or recurring bookings, notifications, reports, and production hosting are outside this refresh. Additional API actions can be planned after the maintenance work is complete.
