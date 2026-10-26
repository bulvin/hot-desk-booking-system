using System.Net;
using Domain.Locations;
using Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace IntegrationTests.Endpoints;

public sealed class ProductionHostingTests
{
    [Theory]
    [InlineData(8080, "/health/live", HttpStatusCode.NotFound)]
    [InlineData(8082, "/health/live", HttpStatusCode.OK)]
    [InlineData(8080, "/api/echo", HttpStatusCode.OK)]
    [InlineData(8082, "/api/echo", HttpStatusCode.NotFound)]
    public async Task Probes_OnlyHealthRoutesAreExposedOnManagementPort(int port, string path, HttpStatusCode status)
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["HealthChecks:Port"] = "8082"
        });
        builder.Services.AddHealthChecks();
        await using var app = builder.Build();
        app.Use(async (context, next) =>
        {
            context.Connection.LocalPort = port;
            await next(context);
        });
        app.UsePrivateHealthChecks();
        app.MapGet("/api/echo", () => "OK");
        await app.StartAsync(TestContext.Current.CancellationToken);
        using var client = app.GetTestClient();

        // Act
        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(status, response.StatusCode);
    }

    [Theory]
    [InlineData("AllowedHosts", "*")]
    [InlineData("AllowedHosts", "")]
    [InlineData("ForwardedHeaders_Enabled", "true")]
    [InlineData("Keycloak:Authority", "http://identity.example.test/realms/hotdesk")]
    [InlineData("Keycloak:BackchannelAuthority", "http://keycloak/realms/hotdesk")]
    [InlineData("Keycloak:RequireHttpsMetadata", "false")]
    [InlineData("Database:SeedSampleData", "true")]
    [InlineData("ReverseProxy:KnownProxies:0", "invalid")]
    [InlineData("ReverseProxy:KnownProxies:0", null)]
    [InlineData("ReverseProxy:KnownNetworks:0", "0.0.0.0/0")]
    [InlineData("ReverseProxy:ForwardLimit", "0")]
    [InlineData("Keycloak:IntrospectionTimeoutSeconds", "0")]
    public void Startup_InvalidProductionConfiguration_RejectsConfiguration(string key, string? value)
    {
        // Arrange
        var configuration = ValidConfiguration();
        if (value is null)
            configuration.Remove(key);
        else
            configuration[key] = value;
        using var factory = new ProductionApplication(configuration);

        // Act
        var exception = Record.Exception(() => factory.CreateClient());

        // Assert
        Assert.NotNull(exception);
        Assert.True(exception is InvalidOperationException or Microsoft.Extensions.Options.OptionsValidationException);
    }

    [Theory]
    [InlineData("192.0.2.10", "https", false)]
    [InlineData("192.0.2.20", "http", false)]
    [InlineData("192.0.2.10", "https", true)]
    [InlineData("192.0.2.20", "http", true)]
    public async Task ForwardedHeaders_OnlyTrustedProxyCanChangeClientAndScheme(string peer, string expectedScheme, bool useNetwork)
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Production });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        var configuration = ValidConfiguration();
        if (useNetwork)
        {
            configuration.Remove("ReverseProxy:KnownProxies:0");
            configuration["ReverseProxy:KnownNetworks:0"] = "192.0.2.8/30";
        }
        builder.Configuration.AddInMemoryCollection(configuration);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["RateLimiting:IpPermitLimit"] = "1"
        });
        builder.Services.AddProductionHosting(builder.Configuration, builder.Environment);
        // This isolated host exercises proxy middleware; the full application tests cover database startup.
        builder.Services.Remove(builder.Services.Single(service => service.ImplementationType == typeof(DatabaseInitializationHostedService)));
        builder.Services.AddApiRateLimiting(builder.Configuration);
        builder.Services.AddProblemDetails();
        await using var app = builder.Build();
        app.Use(async (context, next) =>
        {
            context.Connection.RemoteIpAddress = IPAddress.Parse(peer);
            await next(context);
        });
        app.UseForwardedHeaders();
        app.UseApiIpRateLimiting();
        app.MapGet("/api/echo", (Microsoft.AspNetCore.Http.HttpContext context) => context.Request.Scheme);
        await app.StartAsync(TestContext.Current.CancellationToken);
        using var client = app.GetTestClient();
        client.BaseAddress = new Uri("http://api.example.test");
        client.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "198.51.100.1");

        // Act
        using var first = await client.GetAsync("/api/echo", TestContext.Current.CancellationToken);
        client.DefaultRequestHeaders.Remove("X-Forwarded-For");
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "198.51.100.2");
        using var second = await client.GetAsync("/api/echo", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expectedScheme, await first.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(string.Equals(expectedScheme, "https", StringComparison.Ordinal) ? HttpStatusCode.OK : HttpStatusCode.TooManyRequests,
            second.StatusCode);
    }

    [Fact]
    public async Task Production_StartupAppliesMigrationsAndProbesTrackDependenciesWithoutExposingSwagger()
    {
        // Arrange
        await using var postgres = new PostgreSqlBuilder("postgres:18").Build();
        await postgres.StartAsync(TestContext.Current.CancellationToken);
        var configuration = ValidConfiguration();
        configuration["ConnectionStrings:Database"] = postgres.GetConnectionString();
        await using var factory = new ProductionApplication(configuration);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://api.example.test") });
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-token");
        await using var database = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(postgres.GetConnectionString()).Options);

        // Act
        using var live = await client.GetAsync("/health/live", TestContext.Current.CancellationToken);
        var applied = await database.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);
        var pending = await database.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken);
        var locations = await database.Locations.CountAsync(TestContext.Current.CancellationToken);
        var desks = await database.Desks.CountAsync(TestContext.Current.CancellationToken);
        using var ready = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);
        factory.IdentityAvailable = false;
        using var identityDown = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);
        factory.IdentityAvailable = true;
        using var recovered = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);
        client.DefaultRequestHeaders.Authorization = null;
        using var swagger = await client.GetAsync("/swagger/v1/swagger.json", TestContext.Current.CancellationToken);
        await postgres.StopAsync(TestContext.Current.CancellationToken);
        using var databaseDown = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);
        using var stillLive = await client.GetAsync("/health/live", TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEmpty(applied);
        Assert.Empty(pending);
        Assert.Equal(0, locations);
        Assert.Equal(0, desks);
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, identityDown.StatusCode);
        Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, swagger.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, databaseDown.StatusCode);
        Assert.Equal("Unhealthy", await databaseDown.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(HttpStatusCode.OK, stillLive.StatusCode);
    }

    [Fact]
    public async Task Production_RestartPreservesDataAndMigrationHistory()
    {
        // Arrange
        await using var postgres = new PostgreSqlBuilder("postgres:18").Build();
        await postgres.StartAsync(TestContext.Current.CancellationToken);
        var configuration = ValidConfiguration();
        configuration["ConnectionStrings:Database"] = postgres.GetConnectionString();
        await using var database = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(postgres.GetConnectionString()).Options);
        var location = new Location
        {
            Id = Guid.NewGuid(),
            Name = "Existing office",
            Address = new Address("Office Street", "1", "Warsaw", "00-001")
        };

        // Act
        await using (var first = new ProductionApplication(configuration))
        {
            using var client = first.CreateClient();
            database.Locations.Add(location);
            await database.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var initialHistory = (await database.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)).ToArray();
        await using var restarted = new ProductionApplication(configuration);
        using var restartedClient = restarted.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://api.example.test") });
        using var ready = await restartedClient.GetAsync("/health/ready", TestContext.Current.CancellationToken);
        var finalHistory = (await database.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)).ToArray();
        var persisted = await database.Locations.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEmpty(initialHistory);
        Assert.Equal(initialHistory, finalHistory);
        Assert.Equal(location.Id, persisted.Id);
        Assert.Equal(location.Name, persisted.Name);
        Assert.Equal(location.Address, persisted.Address);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
    }

    private static Dictionary<string, string?> ValidConfiguration() => new(StringComparer.Ordinal)
    {
        ["AllowedHosts"] = "api.example.test",
        ["ReverseProxy:KnownProxies:0"] = "192.0.2.10",
        ["ConnectionStrings:Database"] = "Host=localhost;Database=unused;Username=unused;Password=unused",
        ["Keycloak:Authority"] = "https://identity.example.test/realms/hotdesk",
        ["Keycloak:ClientSecret"] = "test-only",
        ["Keycloak:RequireHttpsMetadata"] = "true",
        ["Database:SeedSampleData"] = "false"
    };

    private sealed class ProductionApplication(Dictionary<string, string?> configuration) : WebApplicationFactory<Program>
    {
        public bool IdentityAvailable { get; set; } = true;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(Environments.Production);
            foreach (var entry in configuration)
                builder.UseSetting(entry.Key, entry.Value);
            builder.ConfigureServices(services => services.ConfigureHttpClientDefaults(client =>
                client.ConfigurePrimaryHttpMessageHandler(() => new DiscoveryHandler(this))));
        }
    }

    private sealed class DiscoveryHandler(ProductionApplication application) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(application.IdentityAvailable ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("{\"issuer\":\"https://identity.example.test/realms/hotdesk\"}", System.Text.Encoding.UTF8, "application/json")
            });
    }
}
