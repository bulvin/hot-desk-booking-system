# Tech stack

See [mission.md](mission.md) for the product scope and [roadmap.md](roadmap.md) for planned work.

## Main technologies

| Technology | What it does |
| --- | --- |
| C# and .NET | Run the application and its booking rules |
| ASP.NET Core | Provides the API that company applications use to book desks |
| ASP.NET Core Identity and JWT | Manage local accounts/password hashes and roles; signed JWTs with database-backed, rotating refresh sessions |
| PostgreSQL | Stores users in AspNetUsers, booking history, and hashed refresh credentials |
| Entity Framework Core and Npgsql | Let the application read and save data in PostgreSQL |
| Docker Compose | Starts the API and database together for development |
| Mailpit | Retained development SMTP utility; unused by the current account workflow |
| Serilog | Writes structured application logs and HTTP request summaries to the console and Seq |
| Seq | Collects structured logs for searching and analysis during local development |
| Swagger | Shows the available API actions and lets developers try them |
| xUnit and Moq | Test booking rules and other application behavior |

Package versions are defined in the project files; the SDK version is defined in `global.json`.

## How the code is organized

The application has four parts, each with a separate responsibility:

- **Domain:** describes desks, locations, reservations, and the core business rules.
- **Application:** coordinates actions such as booking a desk and checks incoming requests.
- **Infrastructure:** handles database access, login support, and other external services.
- **Web.Api:** receives requests from other applications and returns results or clear errors.


## Frontend

The standalone React/TypeScript application uses Vite, React Router, TanStack Query, in-memory JWT access tokens and HttpOnly refresh cookies, and Tailwind CSS with daisyUI. See [frontend.md](frontend.md) for configuration and dependency decisions.

## API rate limiting

The API uses the .NET shared framework's in-memory fixed-window rate limiters; no additional package or service is required. Limits apply to `/api` and every subpath, including unknown routes. Swagger is excluded.

| Configuration key | Default | Meaning |
| --- | --- | --- |
| RateLimiting:IpPermitLimit | 1200 | Requests per connection IP before authentication |
| RateLimiting:UserPermitLimit | 120 | Total requests per authenticated local user across all API routes |
| RateLimiting:WritePermitLimit | 20 | POST, PUT, PATCH and DELETE requests per user across all API routes |
| RateLimiting:WindowSeconds | 60 | Fixed-window duration in seconds |

## Local authentication dependency decision

`Microsoft.AspNetCore.Identity.EntityFrameworkCore` 10.0.12 is approved and aligned with the existing framework/EF package versions. It provides maintained password hashing, lockout, roles, and PostgreSQL persistence through the existing Npgsql provider. Existing JwtBearer/IdentityModel packages validate and issue standard HS256 JWTs. Identity's proprietary bearer-token endpoints are not used. No MailKit dependency is added because email workflows are deferred. Remove `keycloak-js` and `Testcontainers.Keycloak`; no new frontend dependency is needed.

`RateLimiting:AuthPermitLimit` defaults to 30 requests per connection IP per window for `/api/auth` endpoints. Local sessions are checked on each authenticated request, preserving immediate revocation without an external introspection service.

## Unified Identity user

`Domain.Users.User` inherits `IdentityUser<Guid>` and maps to `HotDeskBooking.AspNetUsers`. Reservation ownership, sessions, and Identity roles use the same user ID. Domain intentionally references the existing `Microsoft.AspNetCore.App` shared framework for Identity abstractions; no additional NuGet dependency is added. The separate booking profile entity is removed. An additive migration moves legacy profiles while preserving reservations and account credentials; legacy profiles without credentials become inactive accounts without passwords or roles.
