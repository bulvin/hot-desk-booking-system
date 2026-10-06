# Tech stack

This file explains the main technologies in plain language. For what the product does, see [product.md](product.md). For the order of planned work, see [roadmap.md](roadmap.md).

## Main technologies

| Technology | What it does |
| --- | --- |
| C# and .NET | Run the application and its booking rules |
| ASP.NET Core | Provides the API that company applications use to book desks |
| PostgreSQL | Stores accounts, offices, desks, and reservations |
| Entity Framework Core and Npgsql | Let the application read and save data in PostgreSQL |
| Docker Compose | Starts the API and database together for development |
| Swagger | Shows the available API actions and lets developers try them |
| xUnit and Moq | Test booking rules and other application behavior |

Exact package versions belong in the project files. Choose them during implementation after checking compatibility, support, security, and licensing.

## How the code is organized

The application has four parts, each with a separate responsibility:

- **Domain:** describes desks, locations, reservations, and the core business rules.
- **Application:** coordinates actions such as booking a desk and checks incoming requests.
- **Infrastructure:** handles database access, login support, and other external services.
- **Web.Api:** receives requests from other applications and returns results or clear errors.
