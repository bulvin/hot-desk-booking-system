# hot-desk-booking-system

A .NET 10 desk-booking API with PostgreSQL and Keycloak authentication.

## Local setup

1. Configure `.secrets/postgres_password.txt`; PostgreSQL reads this existing Docker Secret through `POSTGRES_PASSWORD_FILE`. Create the ignored `.secrets/keycloak.conf` with `db-password` matching that file and a separate bootstrap administrator password:

```properties
db-password=<same password as postgres_password.txt>
bootstrap-admin-password=<administrator password>
```

2. Compose mounts that configuration as `/run/secrets/keycloak.conf`; Keycloak reads it natively through `--config-file`, which requires a `.conf` extension. Unlike PostgreSQL, Keycloak does not have a `KC_DB_PASSWORD_FILE` setting, so its password must be specified in this configuration. Escape backslashes and dollar signs according to [Keycloak configuration rules](https://www.keycloak.org/server/configuration). No scripts, .env files or host secret environment variables are required.
3. Start PostgreSQL and create the separate Keycloak database once, before starting Keycloak:

```powershell
docker compose up -d --wait postgres
docker compose exec postgres createdb -U postgres keycloak
docker compose up -d keycloak mailpit
```

Skip `createdb` if the `keycloak` database already exists. Keycloak's first startup can take a few minutes. Create a fresh booking database if required, for example with `docker compose exec postgres createdb -U postgres hot_desk_booking_keycloak`. Do not delete an existing database or its data directory.
4. Open the Keycloak admin console, choose realm `hotdesk`, then Clients Ă˘â€ â€™ `hotdesk-api` Ă˘â€ â€™ Credentials. Copy its client secret (generate one if needed) to the API's .NET User Secrets. For an existing realm, use its existing client secret. Configure the API and run locally:

```powershell
dotnet user-secrets set "Keycloak:ClientSecret" "<client secret from Keycloak>" --project src/Web.Api
dotnet user-secrets set "ConnectionStrings:Database" "Host=localhost;Port=5432;Database=hot_desk_booking_keycloak;Username=postgres;Password=<your PostgreSQL password>" --project src/Web.Api
dotnet run --project src/Web.Api --urls http://localhost:8080
```

For containers use `Host=postgres` in the connection string and `docker compose up -d --build web-api`. The API reads User Secrets from its existing read-only mount and applies EF migrations at startup. Use `docker compose config --quiet` to validate configuration without printing resolved secrets.

| Development page | URL |
| --- | --- |
| Swagger | http://localhost:8080/swagger/ |
| Keycloak administration (`admin`, bootstrap password from `.secrets/keycloak.conf`) | http://localhost:8090/admin/ |
| User account console | http://localhost:8090/realms/hotdesk/account/ |
| Mailpit inbox | http://localhost:8025/ |

Keycloak uses the separate `keycloak` database in the existing `postgres` container, connecting as the development `postgres` user. Booking and Keycloak databases share the existing `.containers/postgres` data directory. Realm import runs only when the realm is absent. Later JSON or client-secret changes require updating the existing realm in the admin console; do not remove PostgreSQL data to apply configuration. See [Keycloak import documentation](https://www.keycloak.org/server/importExport).

If you previously ran the removed `keycloak-db` service, its old `keycloak_data` volume is retained. Existing accounts and sessions require a PostgreSQL dump/restore into the shared server before starting Keycloak; changing the connection URL does not migrate that data.

PostgreSQL password files initialize the database server; they do not change passwords in existing data directories. Keep Keycloak's `db-password` consistent with the existing `postgres` user's actual password.

## Account flow

Use Swagger's **Authorize** button. It redirects to Keycloak using Authorization Code + PKCE. Swagger is a public client and receives no client secret. Register with email and names, open the confirmation email in Mailpit, then set a password and finish logging in. Keycloak's default flow verifies email before password setup. New users receive Employee. Assign Administrator explicitly in Keycloak when needed.

Keycloak hosts registration, login, email confirmation/resend, name editing, password changes and logout. Names are required with maximum 50 characters; email is unique with maximum 100. Passwords require eight characters, uppercase, lowercase, digit and special character. Self-service email changes and password reset are disabled. These operations have no custom API endpoints.

`GET /api/profile` returns ID, email, names, roles and confirmation state. Supply `Authorization: Bearer <access_token>` for profile and booking requests. Reauthorize after editing account details to update token claims. Swagger deauthorization clears only its locally stored token. Log out through Keycloak's account console or the discovery document's OIDC `end_session_endpoint`, with an ID-token hint and an allowed post-logout redirect.

The API validates signature, issuer, audience, expiration and confirmed email, then introspects every accepted token. Ending a Keycloak session therefore invalidates its access token immediately; other sessions remain usable. There is no local session table or token issuer. Authentication requires Keycloak availability. JWT remains the token format; OAuth 2.0/OIDC defines how clients obtain it. See [Keycloak OIDC endpoints](https://www.keycloak.org/securing-apps/oidc-layers).

Keycloak's password-change screen offers **Sign out from other devices**; keep it selected to invalidate other sessions immediately. Administrators can also explicitly sign out all user sessions. Provider account management replaces the previous application-specific atomic password-change/all-session-revocation operation.

## Architecture and migration

The API is an OAuth resource server. Infrastructure contains bearer configuration and token introspection. Local users, Identity stores, password hashing, SMTP delivery, custom authentication services and account handlers are removed. Booking operations retain Mediator, ValidationBehavior, IUnitOfWork and IDateTimeProvider.

Application handlers and the profile endpoint use IUserContext.GetCurrentUser(), returning ProfileDto, for the authenticated subject, names, email, roles and confirmation state. Its scoped Infrastructure implementation snapshots validated claims once per request through IHttpContextAccessor. Application handlers do not read HttpContext or parse claims directly. HttpContextExtensions is removed.

Keycloak owns credentials, email, roles and sessions. A local domain User stores the Keycloak GUID subject as its ID and first/last names. After token validation and introspection, Infrastructure creates a missing local user or updates changed names through IUnitOfWork and IDateTimeProvider. Unchanged data causes no save. Concurrent first requests reuse the same user. Names reflect the latest claims seen by the API; edits become visible when the user returns with an updated token. Reservations have a UserId foreign key and no name snapshots. Administrator desk details load the owner and show the combined name; other users receive no owner data. Ownership checks still compare UserId with IUserContext.GetCurrentUser().Id.

KeycloakAuthentication migrates directly from the original schema: it removes local roles, email and password fields while preserving Users, reservation ownership and profile names. The temporary IdentityAuthentication, KeycloakUserReferences, ReservationUserNames and LocalBookingUsers migrations are removed. Legacy IDs are not automatically relinked to Keycloak subjects. Downgrade restores the original schema with placeholder emails and empty passwords; deleted credentials and role assignments cannot be recovered. Names must fit the old 50-character limit.

| API setting | Meaning |
| --- | --- |
| Keycloak:Authority | Public realm issuer; must match token issuer. |
| Keycloak:BackchannelAuthority | Optional internal realm URL for discovery, signing keys and introspection. |
| Keycloak:Audience | Confidential resource client, default `hotdesk-api`. |
| Keycloak:ClientSecret | Resource-client secret supplied through secrets/environment. |
| Keycloak:SwaggerClientId | Public PKCE client, default `hotdesk-swagger`. |
| Keycloak:RequireHttpsMetadata | True by default, false only for local development. |

Compose's `start-dev`, loopback HTTP and Mailpit SMTP without TLS are development settings. Production TLS/SMTP, frontend, password reset and legacy import are separate work. New clients need explicit redirects/origins and the API audience mapper.

## Validation

```powershell
dotnet restore hot-desk-booking-system.slnx --disable-parallel --tl:off -m:1
dotnet build hot-desk-booking-system.slnx -c Release --no-restore --tl:off -m:1
dotnet test hot-desk-booking-system.slnx -c Release --no-build --no-restore --tl:off -m:1
dotnet format hot-desk-booking-system.slnx --verify-no-changes --no-restore --exclude src/Infrastructure/Data/Migrations --severity info --diagnostics IDE0005 IDE0055 IDE1006
```

Existing tests are adapted; no additional tests were added. See [feature validation](specs/2026-10-10-identity-authentication/validation.md). EF tooling uses the Web.Api startup project and its configured AppDbContext; no design-time factory is needed.
