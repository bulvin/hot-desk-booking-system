## Identity migration removal

IdentityAuthentication and KeycloakUserReferences are removed. KeycloakAuthentication now migrates directly from AddPasswordForUser to the current model, preserving profiles and reservation ownership. The local application database was inspected: only the six original migrations are applied, so no rollback was needed and no application data was modified. All 57 tests pass, including model consistency. Original schema creation, Keycloak upgrade and downgrade SQL succeeded in a disposable PostgreSQL database, which was then removed. Downgrade restores schema with placeholder emails and empty passwords, not deleted credentials or role assignments.

## Current migration correction

KeycloakUserReferences now preserves Users and the reservation FK and widens profile names to text. ReservationUserNames and redundant LocalBookingUsers were removed together with AppDbContextFactory. Its target model matches the current snapshot. Build and all 57 tests pass, including HasPendingModelChanges; EF resolves the context using Web.Api and generates the migration SQL without the factory. No application database was modified. Earlier migration checks below describe superseded designs.

# Keycloak authentication validation

## Local domain user update

- LocalBookingUsers supersedes the earlier no-local-user design and reservation name snapshots described in historical checks below. It restores a minimal user table/FK and backfills existing owners before removing name columns. Synchronization uses UnitOfWork and its time provider, saves only changed/missing profiles, and handles concurrent creation. Names reflect the latest token claims seen by the API.
- Release solution build: zero warnings/errors. All 57 existing tests pass after adapting the desk owner fixture and model relationship assertion; no tests added. Formatting completed and EF reports no pending model changes.
- LocalBookingUsers Up was executed in a uniquely named disposable database in the existing Docker PostgreSQL. Three reservations and two distinct owners survived, the latest non-empty name was copied, a nameless historical owner remained valid, and the user FK was verified. The disposable database was removed.
- The application databases have not been migrated. Real provider checks below predate local-user synchronization; the updated live Keycloak flow has not been rerun.

## Historical automated repository checks (2026-10-10)

- Restore: passed.
- Release solution build: passed, zero warnings/errors.
- Existing tests: 57 passed, zero failed/skipped. No new tests added. Obsolete local JWT/Identity scenarios removed; existing HTTP/Swagger and booking checks adapted.
- IUserContext refactor: HttpContextExtensions removed; booking handlers, desk details and profile endpoint consume the single IUserContext.GetCurrentUser() method returning ProfileDto. Scoped Infrastructure UserContext validates the authenticated GUID and snapshots names, email, roles and confirmation at construction. Existing test fixtures mock GetCurrentUser(); no new tests or database migration required. AppDbContext exposes non-null DbSet properties through Set<T>(). Release build passes with zero warnings/errors and all 57 existing tests pass after the contract simplification.
- Local-user simplification: Release build passes with zero warnings/errors and all 57 existing tests pass. Model assertions verify UserId is a scalar without a user FK; desk-detail assertions verify administrators see the reserving subject ID and other users do not. No tests added.
- Administrator name correction: BookDeskHandler saves given_name/family_name claims on the reservation. Existing desk-detail tests now assert the administrator receives both ID and combined name while other users receive no reserving-user object. Release build, all 57 existing tests and formatting verification pass; no tests added. ReservationUserNames adds nullable varchar(50) name columns, keeping older reservations valid with null names. This migration was generated and model consistency verified by existing tests; it was not applied to the application database.
- KeycloakUserReferences migration checked on a uniquely named disposable database in local PostgreSQL 16: existing reservation and UserId preserved, Users removed, UserId index retained, and a reservation with a new subject inserted without a local user row. Temporary database removed; application databases were not migrated.
- Formatting verification: passed with IDE0005, IDE0055 and IDE1006, excluding generated migrations.
- Docker Compose configuration validation: passed.
- Shared PostgreSQL correction: removed keycloak-db and its Compose volume declaration; Keycloak connects to postgres:5432/keycloak as postgres. Ordinary Compose validation passed. The existing PostgreSQL started healthy; a separate keycloak database was created and a direct connection verified. No previous Keycloak containers/volumes were present to migrate, and booking data was retained.
- Configuration correction: no .env, helper script or host secret environment variables. The API retains its read-only User Secrets mount. The existing PostgreSQL container uses postgres_password and hosts separate booking and keycloak databases. Keycloak reads keycloak_config through --config-file; copy its confidential client credential to the API's User Secret. Validate with ordinary docker compose config --quiet. The removed keycloak-db service's data volume must be retained until any existing account data is migrated.

## Manual provider checks (2026-10-10)

Validated with disposable PostgreSQL 18, Keycloak 26.7.5 and Mailpit containers, plus the Release API on a separate validation database. Real browser-form HTTP flow and Authorization Code + S256 PKCE were used. Password grants were used only for the disposable Keycloak admin CLI to arrange fixtures; application clients have direct grants disabled. Tokens and confirmation links were held in memory, not recorded in output.

- Final realm imports on a fresh realm; Keycloak starts successfully. Basic, profile, email and roles scopes yield a GUID subject, verified email, names, Employee role and hotdesk-api audience.
- Registration requires email confirmation, Mailpit receives a usable link, and Keycloak asks for password setup after confirmation.
- Authorization-code exchange with the PKCE verifier succeeds; GET /api/profile returns the expected account data.
- Anonymous profile request returns 401; Employee creating a location receives 403.
- Authentication no longer provisions users or accesses the booking database. Reservations persist the GUID subject directly through UnitOfWork.
- Standard OIDC logout makes the current access token return 401 immediately; a separately authenticated session remains valid.
- Account API access succeeds; changing first/last names is reflected in the API after reauthorization.
- Hosted UPDATE_PASSWORD flow with Sign out from other devices rejects the earlier session. The new password supports a fresh authorized request.
- Public issuer localhost and distinct backchannel 127.0.0.1 work together, including signing-key retrieval and introspection.
- Administrator can create a location and desk, then book that desk; the reservation stores the Keycloak subject in UserId without a local user FK. New bookings also store name snapshots from token claims for administrator display; older bookings without snapshots return a null name.
- The complete historical EF migration chain applies to a fresh database. KeycloakUserReferences then removes Users and the reservation-user FK, preserving booking records and their owner IDs. The final schema contains locations, desks and reservations with no account/session/user tables.

## Operational checks and limits

- Keycloak account console owns email/name/password validation and resend behavior. The old API's custom cooldown and SMTP error contracts are removed; provider pages handle their own errors.
- Realm import skips existing realms. Update settings/secrets explicitly in Keycloak rather than deleting its database volume.
- Self-service email editing is admin-only in user-profile configuration; password reset is disabled. Application direct password and implicit grants are disabled.
- Keycloak controls session policy during password changes; Sign out from other devices was explicitly exercised. Immediate logout relies on uncached live introspection, so Keycloak availability is required.
- Legacy users/passwords are not imported. Existing booking IDs remain unchanged, and relinking requires a separate reviewed migration. Do not apply against existing accounts without backup/import planning. Down migration cannot restore dropped credential data.
- Production TLS/SMTP, frontend, legacy import, exhaustive provider validation, load/concurrency testing and browser UI automation are outside this change. Manual HTTP checks do not replace these.
