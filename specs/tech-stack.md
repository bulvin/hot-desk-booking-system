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
| React and TypeScript | Build the browser interface with typed components |
| Vite | Runs the frontend development server and builds the application |
| React Router | Handles navigation between frontend pages |
| TanStack Query | Fetches API data and manages its cache |
| Tailwind CSS and daisyUI | Style the interface with utilities and reusable components |
| Vitest and Testing Library | Test frontend components and user workflows |


## How the code is organized

The application has four parts, each with a separate responsibility:

- **Domain:** describes desks, locations, reservations, and the core business rules.
- **Application:** coordinates actions such as booking a desk and checks incoming requests.
- **Infrastructure:** handles database access, login support, and other external services.
- **Web.Api:** receives requests from other applications and returns results or clear errors.

## API rate limiting

The API uses the .NET shared framework's in-memory fixed-window rate limiters; no additional package or service is required. Limits apply to `/api` and every subpath, including unknown routes. Swagger is excluded.

| Configuration key | Default | Meaning |
| --- | --- | --- |
| RateLimiting:IpPermitLimit | 1200 | Requests per connection IP before authentication |
| RateLimiting:UserPermitLimit | 120 | Total requests per authenticated local user across all API routes |
| RateLimiting:WritePermitLimit | 20 | POST, PUT, PATCH and DELETE requests per user across all API routes |
| RateLimiting:WindowSeconds | 60 | Fixed-window duration in seconds |
