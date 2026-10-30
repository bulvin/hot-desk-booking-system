# Local development

Requires Docker Compose (Linux containers), Node.js 24, and the .NET SDK specified in `global.json`. Run the shell commands below from the repository root, for example in Git Bash on Windows. The current Compose configuration mounts user secrets from Windows `%APPDATA%`.

## 1. Configure local secrets

Replace the placeholders below with your own values. Use the same database password in both places and a random JWT signing key of at least 32 bytes. For an existing database, use its current password.

```sh
mkdir -p .secrets
printf '%s' '<database-password>' > .secrets/postgres_password.txt
dotnet user-secrets set 'ConnectionStrings:Database' 'Host=postgres;Port=5432;Database=hot_desk_booking;Username=postgres;Password=<database-password>' --project src/Web.Api
dotnet user-secrets set 'Jwt:SigningKey' '<random-signing-key-at-least-32-bytes>' --project src/Web.Api
```

The `.secrets` directory is ignored by Git. API secrets are stored by .NET outside the repository.

## 2. Start the API in Docker

```sh
docker compose up --build -d
```

The API runs at `http://localhost:8080`; no certificate is required. It applies database migrations and seeds local demo data on startup. Database files persist in `.containers/postgres`. Re-run the command after backend changes to rebuild the API.

## 3. Start the frontend

In another terminal, from the repository root:

```sh
cd frontend
npm ci
npm run dev
```

No frontend environment file is required for this Docker setup.

## Optional: HTTPS for the API

To also expose `https://localhost:8081`, run the helper from the repository root. It asks for a certificate password, exports and trusts the development certificate, and starts Compose with the HTTPS override:

```sh
bash scripts/start-dev-https.sh
```

For subsequent HTTPS rebuilds, use:

```sh
docker compose -f docker-compose.yml -f docker-compose.override.yml -f docker-compose.https.yml up --build -d
```

Certificates are stored in the Git-ignored `.https` directory. The frontend can continue using HTTP port `8080`.

## Alternative: run the API from source

After configuring secrets above, stop the API container and change the database connection to the host port:

```sh
docker compose stop web-api
dotnet user-secrets set 'ConnectionStrings:Database' 'Host=localhost;Port=5433;Database=hot_desk_booking;Username=postgres;Password=<database-password>' --project src/Web.Api
docker compose up -d postgres seq
dotnet run --project src/Web.Api --launch-profile http
```

In the frontend terminal, point Vite at the API's source-run port, `5080`:

```sh
cd frontend
npm ci
API_PROXY_TARGET=http://localhost:5080 npm run dev
```

When switching back to Docker, restore `Host=postgres;Port=5432` in the database connection and restart the frontend with plain `npm run dev`.

## Local demo

Open `http://localhost:5173`. Swagger is at `/swagger` on API port `8080` (Docker) or `5080` (source).

Development startup creates `admin@hotdesk.example` and `employee@hotdesk.example`, both with password `LocalDemo123!`, when sample data is enabled.
