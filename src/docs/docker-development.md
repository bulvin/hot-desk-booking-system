# Local Docker development (.NET 10 / PostgreSQL 18)

Run commands below in PowerShell from `src`. Prerequisites: Docker Desktop running Linux containers, Docker Compose v2 or newer, and the .NET SDK selected by repository-root `global.json` for User Secrets and local checks.

## Configure and start

Compose builds from the repository root. `Web.Api/Dockerfile.dockerignore` limits the context to `global.json` and application source, excluding build outputs and local secrets/data. Images track .NET 10.0 and PostgreSQL 18 using tags `sdk:10.0`, `aspnet:10.0`, and `postgres:18`. The SDK policy permits newer .NET 10.0 feature bands.

Store the database password in `.secrets/postgres_password.txt` as UTF-8 without a BOM or trailing newline. Set the API's matching connection string and JWT options in User Secrets; do not put values in tracked settings. For example, generate new local credentials on a fresh installation:

```powershell
New-Item -ItemType Directory -Force .secrets | Out-Null
$dbPassword = [Guid]::NewGuid().ToString('N') + [Guid]::NewGuid().ToString('N')
$jwtKey = [Guid]::NewGuid().ToString('N') + [Guid]::NewGuid().ToString('N')
[IO.File]::WriteAllText((Join-Path $PWD '.secrets/postgres_password.txt'), $dbPassword)
dotnet user-secrets set 'ConnectionStrings:Database' "Host=postgres;Port=5432;Database=hot_desk_booking;Username=postgres;Password=$dbPassword" --project Web.Api
dotnet user-secrets set 'Jwt:Key' $jwtKey --project Web.Api
dotnet user-secrets set 'Jwt:Expires' '1' --project Web.Api
```

Do not regenerate the password for an already initialized database: `POSTGRES_PASSWORD_FILE` configures initialization and does not change an existing database user's password. Reuse its existing credential instead. `Jwt:Expires` is measured in days by the existing implementation. These User Secrets also affect host launches; use `Host=localhost` and the mapped database port when running the API outside Docker.

The Windows development override mounts `${APPDATA}/Microsoft/UserSecrets` read-only into the non-root API user's home. On other hosts, supply an appropriate read-only mount or equivalent environment configuration. The API connects to `postgres:5432` over the Compose network, regardless of the host port.

For a side-by-side rehearsal with the old stack, use a separate Compose project and unused host ports (retain this environment setting for all commands below):

```powershell
$env:API_PORT = 'hot_desk_booking_upgrade'
 = '8081'
$env:POSTGRES_PORT = '5433'
docker compose config --quiet
docker compose up -d --build
docker compose ps
docker compose logs --tail 50 web-api
```

Defaults are loopback ports 8080 and 5432. Open `http://localhost:8081/swagger` when using the example ports. PostgreSQL readiness gates API startup; wait for the API's `Now listening` log before sending requests. `up -d` alone is not an HTTP readiness check.

The default project name remains `hot_desk_booking` and the API image remains `web.api`. Generated container names permit an alternate project for upgrade rehearsals; without that override, Compose updates the existing project. Before rebuilding `web.api`, tag the old image as `web.api:rollback` if you need to retain it for rollback. PostgreSQL 18 stores files under `/var/lib/postgresql/18/docker`, inside the new `src/.containers/db18` bind mount at `/var/lib/postgresql`. The old `src/.containers/db` is preserved.

The API applies EF migrations and seeds roles/an initial administrator at startup. The existing development administrator credentials remain defined in `Infrastructure/Data/DbSeeder.cs`; production credential/bootstrap hardening is outside this phase.

## Migrate existing PostgreSQL 17 data

A major PostgreSQL upgrade requires a logical dump/restore or `pg_upgrade`; EF migrations alone cannot upgrade the server's storage format. The verified path here is a PostgreSQL 18 logical dump of 17 and restore into a separate empty 18 database.

1. Stop application writes to the old database during the final export/cutover. Keep its PostgreSQL 17 container and data directory intact. For rehearsal, use synthetic data or a disposable logical copy.
2. Start only the new database: `docker compose up -d postgres`. Do not start the new API before restore; it would create tables and seed data in the empty database.
3. Export with PostgreSQL 18 tooling. The following assumes the old database container is named `postgres`, with user `postgres` and database `hot_desk_booking`. Adapt these identifiers if different. Set `$env:PGPASSWORD` locally to its actual password without printing it.

```powershell
New-Item -ItemType Directory -Force .containers/backups | Out-Null
$backupPath = (Resolve-Path .containers/backups).Path
docker run --rm --network container:postgres -e PGPASSWORD --mount "type=bind,source=$backupPath,target=/backup" postgres:18 pg_dump -h 127.0.0.1 -U postgres -d hot_desk_booking --format=custom --file=/backup/postgres17.dump
if ($LASTEXITCODE -ne 0) { throw 'Database export failed' }
```

4. Set `$env:PGPASSWORD` to the new database's configured password and restore into its empty `hot_desk_booking` database. This database is initialized by the image, but must not yet contain API tables. Do not add `--clean` to overwrite an existing target.

```powershell
$targetContainer = docker compose ps -q postgres
docker run --rm --network "container:$targetContainer" -e PGPASSWORD --mount "type=bind,source=$backupPath,target=/backup,readonly" postgres:18 pg_restore -h 127.0.0.1 -U postgres -d hot_desk_booking --exit-on-error /backup/postgres17.dump
if ($LASTEXITCODE -ne 0) { throw 'Database restore failed; do not start the API' }
Remove-Item Env:PGPASSWORD
docker compose up -d --build web-api
```

5. Compare row counts and values for users, roles, user-role relationships, locations, desks, reservations, and `__EFMigrationsHistory`. Verify existing login, desk details, and reservation dates/IDs through the API. Restart both new services and compare again to detect duplicate seeds or data loss.
6. Cut over only after validation. Before accepting writes on 18, rollback is to stop the new API and restart the original 17 API/database configuration. Writes accepted on 18 are not automatically copied back to 17; rollback after new writes needs a separate reconciliation plan.

The tested fixture used the old .NET 8 image on PostgreSQL 17 to create a location, desk, employee, and reservation, then restored its full database using PostgreSQL 18 tooling. Actual company data was not accessed or migrated.

## Verify and stop

Check Swagger JSON (`/swagger/v1/swagger.json`), registration/login, an authenticated `GET /api/locations/{locationId}/desks`, and a 401 response for the same request without a token. Login returns a plain-text bearer token; registration returns a JSON user ID. Expect 200 for login/list/registration and 201 for location/desk creation.

```powershell
docker compose restart postgres web-api
dotnet test hot-desk-booking-system.sln --configuration Release --disable-build-servers -m:1
docker compose down
```

`down` removes this project's containers/network and preserves the bind-mounted database. Do not delete `.containers/db` or `.containers/db18` to troubleshoot. For isolated test stacks, remove only explicitly named test containers/volumes after recording evidence. Database dumps contain data and must remain ignored/unshared.

Known follow-ups: the existing AutoMapper vulnerability and nullable warnings remain tracked in later phases. Passing these Docker checks does not establish full production readiness.
