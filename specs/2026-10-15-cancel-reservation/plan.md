# Cancel reservation

## CONTEXT

Phase 11 adds owner and administrator cancellation. Reservations already have a `Canceled` status. Availability and desk deletion must handle retained history consistently.

## GOAL

- Add `PUT /api/reservations/{id:guid}/cancel` with no body; return `204` on success, `401` without authentication, `403` for another employee's reservation, `404` if missing, and `409` for a prohibited state or cutoff.
- Owners may cancel through the start day in UTC. For a Friday booking, cancellation is allowed throughout Friday and closes at 00:00 UTC on Saturday.
- Administrators may cancel reserved bookings through their final UTC day. Completed bookings cannot be canceled.
- Set status to `Canceled`, retain history, and free the entire date range. An authorized repeat returns `204` without writes, even after the cutoff. Canceled reservations cannot be modified through desk changes, including stale concurrent requests.
- Block deletion of desks with any reservation history with `409`.
- Use the existing architecture, Mediator, UTC clock, and persistence patterns. Status concurrency metadata needs no database column changes; existing data and migrations are preserved.

## DEFINITION OF DONE

- Rules, permissions, persistence, availability queries, and history protection work consistently.
- Regression tests cover UTC boundaries, owners, administrators, missing reservations, non-owner denial, completed states, repeats, and changes to canceled reservations.
- PostgreSQL tests verify persistence, both bookability filters, desk details, retained history, deletion protection, and stale concurrent desk changes.
- Swagger and the mission describe the agreed rules; mark Phase 11 complete after verification.
- Restore, Release build without warnings, tests, and formatting verification pass. No dependencies are added.
