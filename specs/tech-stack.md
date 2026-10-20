# Tech stack

See [mission.md](mission.md) for the product scope and [roadmap.md](roadmap.md) for planned work.

## Main technologies

| Technology | What it does |
| --- | --- |
| C# and .NET | Run the application and its booking rules |
| ASP.NET Core | Provides the API that company applications use to book desks |
| Keycloak, OAuth 2.0 and OpenID Connect | Manage accounts and roles; Authorization Code + PKCE supplies access tokens, validated and introspected by the API |
| PostgreSQL | Stores offices, desks and reservations with Keycloak subject IDs; Keycloak stores accounts in a separate database in the same container |
| Entity Framework Core and Npgsql | Let the application read and save data in PostgreSQL |
| Docker Compose | Starts the API and database together for development |
| Mailpit | Captures development email confirmation messages through SMTP |
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
