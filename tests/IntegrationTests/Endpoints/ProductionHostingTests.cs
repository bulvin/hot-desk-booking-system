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
using Testcontainers.PostgreSql;
using Web.Api.Extensions;

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
