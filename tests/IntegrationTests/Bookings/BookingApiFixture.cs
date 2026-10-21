using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Application.Interfaces;
using Domain.Desks;
using Domain.Locations;
using Infrastructure.Data;
using Infrastructure.Time;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.Keycloak;
using Testcontainers.PostgreSql;

namespace IntegrationTests.Bookings;

public sealed class BookingApiFixture : IAsyncLifetime
{
    public static readonly Guid EmployeeId = new("ce9e947b-4bb4-490f-8e56-624dfabfb803");
    public static readonly DateOnly Today = new(2026, 10, 16);
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18")
        .WithCreateParameterModifier(parameters =>
        {
            parameters.HostConfig ??= new();
            parameters.HostConfig.Memory = 256L * 1024 * 1024;
            parameters.HostConfig.MemorySwap = parameters.HostConfig.Memory;
            parameters.HostConfig.NanoCPUs = 1_000_000_000;
        })
        .Build();
    private readonly string _realmDirectory = Path.Combine(Path.GetTempPath(), $"hotdesk-tests-{Guid.NewGuid():N}");
    private readonly string _password = $"Test-{Guid.NewGuid():N}!";
    private readonly string _clientSecret = Guid.NewGuid().ToString("N");
    private KeycloakContainer? _keycloak;
    private WebApplicationFactory<Program>? _factory;
    private string _accessToken = string.Empty;

    public async ValueTask InitializeAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(_realmDirectory);
        var realmPath = Path.Combine(_realmDirectory, "hotdesk-realm.json");
        var realm = JsonNode.Parse(await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "Integration", "hotdesk-realm.json"), cancellationToken))!;
        realm["sslRequired"] = "none";
        var api = realm["clients"]!.AsArray().Single(client => string.Equals(client!["clientId"]!.GetValue<string>(), "hotdesk-api", StringComparison.Ordinal))!;
        // Password grant is restricted to this disposable test realm; production keeps PKCE.
        api["directAccessGrantsEnabled"] = true;
        api["secret"] = _clientSecret;
        api["defaultClientScopes"] = new JsonArray("basic", "roles", "profile", "email");
        api["protocolMappers"] = new JsonArray(JsonSerializer.SerializeToNode(new
        {
            name = "api-audience",
            protocol = "openid-connect",
            protocolMapper = "oidc-audience-mapper",
            config = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["included.client.audience"] = "hotdesk-api",
                ["access.token.claim"] = "true"
            }
        }));
        realm["users"] = new JsonArray(JsonSerializer.SerializeToNode(new
        {
            id = EmployeeId,
            username = "employee@example.test",
            email = "employee@example.test",
            firstName = "Booking",
            lastName = "Employee",
            enabled = true,
            emailVerified = true,
            realmRoles = new[] { "Employee" },
            credentials = new[] { new { type = "password", value = _password, temporary = false } }
        }));
        await File.WriteAllTextAsync(realmPath, realm.ToJsonString(), cancellationToken);
        _keycloak = new KeycloakBuilder("quay.io/keycloak/keycloak:26.8")
            .WithRealm(realmPath)
            .WithEnvironment("JAVA_OPTS_KC_HEAP", "-Xms64m -Xmx512m")
            .WithEnvironment("JAVA_OPTS_APPEND", "-XX:ActiveProcessorCount=2")
            .WithCreateParameterModifier(parameters =>
            {
                parameters.HostConfig ??= new();
                // Leave room beyond the 512 MiB Java heap for native memory and startup.
                parameters.HostConfig.Memory = 1024L * 1024 * 1024;
                parameters.HostConfig.MemorySwap = parameters.HostConfig.Memory;
                parameters.HostConfig.NanoCPUs = 2_000_000_000;
            })
            .Build();
        await Task.WhenAll(_postgres.StartAsync(cancellationToken), _keycloak.StartAsync(cancellationToken));
        var authority = $"{_keycloak.GetBaseAddress().TrimEnd('/')}/realms/hotdesk";
        _factory = new BookingApplication(_postgres.GetConnectionString(), authority, _clientSecret);
        // Start the actual application, including its migration hosted service.
        using var startupClient = _factory.CreateClient();
        using var keycloakClient = new HttpClient();
        using var body = new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["grant_type"] = "password",
            ["client_id"] = "hotdesk-api",
            ["client_secret"] = _clientSecret,
            ["username"] = "employee@example.test",
            ["password"] = _password
        });
        using var response = await keycloakClient.PostAsync($"{authority}/protocol/openid-connect/token", body, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var token = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        _accessToken = token.RootElement.GetProperty("access_token").GetString()!;
    }

    public HttpClient CreateClient(bool authenticated = true)
    {
        var client = _factory!.CreateClient();
        if (authenticated)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        return client;
    }

    public AppDbContext CreateDbContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql(_postgres.GetConnectionString()).Options);

    public async Task<Guid> CreateDesk(bool available = true)
    {
        await using var db = CreateDbContext();
        var desk = new Desk
        {
            Name = "Integration desk",
            IsAvailable = available,
            Location = new Location { Name = "Integration office", Address = new Address("Street", "1", "City", "00-001") }
        };
        db.Desks.Add(desk);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return desk.Id;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_factory is not null)
                await _factory.DisposeAsync();
        }
        finally
        {
            try
            {
                if (_keycloak is not null)
                    await _keycloak.DisposeAsync();
            }
            finally
            {
                await _postgres.DisposeAsync();
                if (Directory.Exists(_realmDirectory))
                    Directory.Delete(_realmDirectory, recursive: true);
            }
        }
    }

    private sealed class BookingApplication(string connectionString, string authority, string clientSecret) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("RateLimiting:IpPermitLimit", "100000");
            builder.UseSetting("RateLimiting:UserPermitLimit", "100000");
            builder.UseSetting("RateLimiting:WritePermitLimit", "100000");
            builder.UseSetting("ConnectionStrings:Database", connectionString);
            builder.UseSetting("Database:SeedSampleData", "false");
            builder.UseSetting("Keycloak:Authority", authority);
            builder.UseSetting("Keycloak:BackchannelAuthority", authority);
            builder.UseSetting("Keycloak:ClientSecret", clientSecret);
            builder.UseSetting("Keycloak:Audience", "hotdesk-api");
            builder.UseSetting("Keycloak:RequireHttpsMetadata", "false");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDateTimeProvider>();
                services.AddSingleton<IDateTimeProvider>(new DateTimeProvider
                {
                    UtcNow = new DateTime(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc)
                });
            });
        }
    }
}
