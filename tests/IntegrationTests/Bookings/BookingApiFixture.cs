using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Application.Interfaces;
using Domain.Desks;
using Domain.Locations;
using Domain.Users;
using Infrastructure.Authentication;
using Infrastructure.Data;
using Infrastructure.Time;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace IntegrationTests.Bookings;

public sealed class BookingApiFixture : IAsyncLifetime
{
    public static readonly Guid EmployeeId = new("ce9e947b-4bb4-490f-8e56-624dfabfb803");
    public static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18")
        .WithCreateParameterModifier(parameters =>
        {
            parameters.HostConfig ??= new();
            parameters.HostConfig.Memory = 256L * 1024 * 1024;
            parameters.HostConfig.MemorySwap = parameters.HostConfig.Memory;
            parameters.HostConfig.NanoCPUs = 1_000_000_000;
        })
        .Build();
    private readonly string _password = $"Test-{Guid.NewGuid():N}!";
    private WebApplicationFactory<Program>? _factory;
    private string _accessToken = string.Empty;
    public IServiceProvider Services => _factory!.Services;

    public async ValueTask InitializeAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await _postgres.StartAsync(cancellationToken);
        _factory = new BookingApplication(_postgres.GetConnectionString());
        using var startupClient = CreateClient(authenticated: false);
        await using var scope = Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        Assert.True((await roles.CreateAsync(new IdentityRole<Guid>("Employee"))).Succeeded);
        var account = new User { Id = EmployeeId, Email = "employee@example.test", UserName = "employee@example.test", FirstName = "Booking", LastName = "Employee" };
        Assert.True((await users.CreateAsync(account, _password)).Succeeded);
        Assert.True((await users.AddToRoleAsync(account, "Employee")).Succeeded);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.SaveChangesAsync(cancellationToken);
        using var response = await startupClient.PostAsJsonAsync("/api/auth/login", new { email = account.Email, password = _password }, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var token = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        _accessToken = token.RootElement.GetProperty("accessToken").GetString()!;
    }

    public HttpClient CreateClient(bool authenticated = true)
    {
        var client = _factory!.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
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
        if (_factory is not null)
            await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private sealed class BookingApplication(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("RateLimiting:IpPermitLimit", "100000");
            builder.UseSetting("RateLimiting:UserPermitLimit", "100000");
            builder.UseSetting("RateLimiting:WritePermitLimit", "100000");
            builder.UseSetting("RateLimiting:AuthPermitLimit", "100000");
            builder.UseSetting("ConnectionStrings:Database", connectionString);
            builder.UseSetting("Database:SeedSampleData", "false");
            builder.UseSetting("Jwt:SigningKey", Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDateTimeProvider>();
                services.AddSingleton<IDateTimeProvider>(new DateTimeProvider
                {
                    UtcNow = DateTime.UtcNow
                });
            });
        }
    }
}
