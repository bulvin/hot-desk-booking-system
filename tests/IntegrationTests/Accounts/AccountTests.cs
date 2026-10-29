using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Accounts;
using Application.Accounts.CreateEmployee;
using Application.Dtos;
using Application.Interfaces;
using Domain.Exceptions.Users;
using Domain.Users;
using Infrastructure.Authentication;
using IntegrationTests.Bookings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Accounts;

[Collection("Application host")]
public sealed class AccountTests(BookingApiFixture fixture) : IClassFixture<BookingApiFixture>
{
    [Fact]
    public async Task CreateEmployee_AssignedPassword_IsHashedAndProfileIsPreserved()
    {
        // Arrange
        using var admin = await Administrator();
        var request = NewEmployee();

        // Act
        using var response = await admin.PostAsJsonAsync("/api/users", request, TestContext.Current.CancellationToken);
        var created = await response.Content.ReadFromJsonAsync<AccountDto>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(created);
        await using var db = fixture.CreateDbContext();
        var identity = await db.Users.SingleAsync(x => x.Id == created.Id, TestContext.Current.CancellationToken);
        Assert.NotEqual(request.Password, identity.PasswordHash, StringComparer.Ordinal);
        Assert.False(identity.EmailConfirmed);
        Assert.Equal(request.Email, identity.Email);
        Assert.Equal(request.FirstName, identity.FirstName);
        Assert.Equal(request.LastName, identity.LastName);
        var session = await Login(request);
        using var employee = WithToken(session.Token);
        using var result = await employee.GetAsync("/api/profile", TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(await result.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(created.Id, json.RootElement.GetProperty("id").GetGuid());
        Assert.False(json.RootElement.GetProperty("emailConfirmed").GetBoolean());
    }

    [Fact]
    public async Task CreateEmployee_ConcurrentNormalizedDuplicate_OnlyOneAccountIsCreated()
    {
        // Arrange
        using var admin = await Administrator();
        var request = NewEmployee();

        // Act
        var results = await Task.WhenAll(admin.PostAsJsonAsync("/api/users", request, TestContext.Current.CancellationToken),
            admin.PostAsJsonAsync("/api/users", request with { Email = request.Email.ToUpperInvariant() }, TestContext.Current.CancellationToken));

        // Assert
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.Created);
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.Conflict);
        foreach (var result in results)
            result.Dispose();
    }

    [Fact]
    public async Task Login_FiveFailures_LocksAccountAndDoesNotRevealExistence()
    {
        // Arrange
        var request = await CreateEmployee();
        using var client = fixture.CreateClient(authenticated: false);

        // Act
        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var failed = await client.PostAsJsonAsync("/api/auth/login", request with { Password = "Wrong-password!" }, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        }
        using var locked = await client.PostAsJsonAsync("/api/auth/login", request, TestContext.Current.CancellationToken);
        using var unknown = await client.PostAsJsonAsync("/api/auth/login", NewEmployee(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        var lockedBody = await locked.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var unknownBody = await unknown.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(unknownBody.GetProperty("detail").GetString(), lockedBody.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Refresh_RotationAndReplay_RevokeEntireSession()
    {
        // Arrange
        var request = await CreateEmployee();
        var first = await Login(request);
        using var client = WithCookie(first.Cookie);

        // Act
        using var refreshed = await client.PostAsync("/api/auth/refresh", null, TestContext.Current.CancellationToken);
        var second = await ReadSession(refreshed);
        using var replay = await client.PostAsync("/api/auth/refresh", null, TestContext.Current.CancellationToken);
        using var authenticated = WithToken(second.Token);
        using var profile = await authenticated.GetAsync("/api/profile", TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEqual(first.Cookie, second.Cookie, StringComparer.Ordinal);
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, profile.StatusCode);
        await using var db = fixture.CreateDbContext();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(first.Cookie.Split('=', 2)[1])));
        Assert.True(await db.RefreshCredentials.AnyAsync(x => x.Hash == hash && x.IsUsed, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Refresh_ConcurrentReuse_AtMostOneRotationSucceeds()
    {
        // Arrange
        var first = await Login(await CreateEmployee());
        using var one = WithCookie(first.Cookie);
        using var two = WithCookie(first.Cookie);

        // Act
        var responses = await Task.WhenAll(one.PostAsync("/api/auth/refresh", null, TestContext.Current.CancellationToken),
            two.PostAsync("/api/auth/refresh", null, TestContext.Current.CancellationToken));

        // Assert
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Unauthorized);
        var sessionId = Guid.Parse(new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler()
            .ReadJwtToken(first.Token).Claims.Single(claim => claim.Type is "sid").Value);
        await using var db = fixture.CreateDbContext();
        Assert.True((await db.AuthSessions.SingleAsync(session => session.Id == sessionId,
            TestContext.Current.CancellationToken)).IsRevoked);
        Assert.Equal(2, await db.RefreshCredentials.CountAsync(credential => credential.SessionId == sessionId,
            TestContext.Current.CancellationToken));
        foreach (var response in responses)
            response.Dispose();
    }

    [Fact]
    public async Task Logout_Repeated_RevokesAccessAndClearsCookie()
    {
        // Arrange
        var session = await Login(await CreateEmployee());
        using var client = WithCookie(session.Cookie);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);

        // Act
        using var first = await client.PostAsync("/api/auth/logout", null, TestContext.Current.CancellationToken);
        using var second = await client.PostAsync("/api/auth/logout", null, TestContext.Current.CancellationToken);
        using var profile = await client.GetAsync("/api/profile", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
        Assert.Contains(first.Headers.GetValues("Set-Cookie"), x => x.StartsWith("hotdesk.refresh=;", StringComparison.Ordinal));
        Assert.Equal(HttpStatusCode.Unauthorized, profile.StatusCode);
    }

    [Fact]
    public async Task Deactivation_RevokesTokens_AndReactivationRequiresLogin()
    {
        // Arrange
        var request = await CreateEmployee();
        var session = await Login(request);
        using var employee = WithToken(session.Token);
        using var admin = await Administrator();
        await using var db = fixture.CreateDbContext();
        var id = await db.Users.Where(x => x.Email == request.Email).Select(x => x.Id).SingleAsync(TestContext.Current.CancellationToken);

        // Act
        using var disabled = await admin.PutAsJsonAsync($"/api/users/{id}/active", new { IsActive = false }, TestContext.Current.CancellationToken);
        using var denied = await employee.GetAsync("/api/profile", TestContext.Current.CancellationToken);
        using var loginDenied = await employee.PostAsJsonAsync("/api/auth/login", request, TestContext.Current.CancellationToken);
        using var enabled = await admin.PutAsJsonAsync($"/api/users/{id}/active", new { IsActive = true }, TestContext.Current.CancellationToken);
        using var oldToken = await employee.GetAsync("/api/profile", TestContext.Current.CancellationToken);
        var newSession = await Login(request);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, disabled.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, loginDenied.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, enabled.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, oldToken.StatusCode);
        Assert.NotEmpty(newSession.Token);
    }

    [Fact]
    public async Task Users_EmployeesCannotManageAccounts_AndAdministratorsCannotBeDeactivated()
    {
        // Arrange
        using var employee = fixture.CreateClient();
        using var admin = await Administrator();
        await using var scope = fixture.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var administrator = (await users.GetUsersInRoleAsync("Administrator")).Single(user => string.Equals(user.Email, "admin@example.test", StringComparison.Ordinal));

        // Act
        using var forbidden = await employee.GetAsync("/api/users", TestContext.Current.CancellationToken);
        using var protectedAdmin = await admin.PutAsJsonAsync($"/api/users/{administrator.Id}/active", new { IsActive = false }, TestContext.Current.CancellationToken);
        using var listed = await admin.GetAsync("/api/users?page=1&pageSize=5&search=admin", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, protectedAdmin.StatusCode);
        Assert.Equal(HttpStatusCode.OK, listed.StatusCode);
    }

    [Fact]
    public async Task ListAccounts_ActiveFirstAndNewestFirst_HasStablePagination()
    {
        // Arrange
        using var admin = await Administrator();
        await using var db = fixture.CreateDbContext();
        var marker = $"ordering-{Guid.NewGuid():N}";
        var now = DateTime.UtcNow;
        var accounts = Enumerable.Range(1, 4).Select(index => new User
        {
            Id = Guid.Parse($"00000000-0000-0000-0000-{index:D12}"),
            Email = $"{marker}-{index}@example.test",
            UserName = $"{marker}-{index}@example.test",
            FirstName = marker,
            LastName = "Ordering",
            IsActive = index != 1,
            CreatedAt = index <= 2 ? now : now.AddDays(-1),
            UpdatedAt = now
        }).ToArray();
        db.Users.AddRange(accounts.Reverse());
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        using var firstResponse = await admin.GetAsync($"/api/users?search={marker}&page=1&pageSize=2", TestContext.Current.CancellationToken);
        using var secondResponse = await admin.GetAsync($"/api/users?search={marker}&page=2&pageSize=2", TestContext.Current.CancellationToken);
        var first = await firstResponse.Content.ReadFromJsonAsync<PagedDto<AccountDto>>(TestContext.Current.CancellationToken);
        var second = await secondResponse.Content.ReadFromJsonAsync<PagedDto<AccountDto>>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(new[] { accounts[1].Id, accounts[2].Id }, first.Items.Select(account => account.Id));
        Assert.Equal(new[] { accounts[3].Id, accounts[0].Id }, second.Items.Select(account => account.Id));
        Assert.Equal(4, first.TotalItems);
        Assert.Equal(4, second.TotalItems);
    }

    [Theory]
    [InlineData("%")]
    [InlineData("_")]
    [InlineData("\\")]
    public async Task ListAccounts_Search_IsCaseInsensitiveAndTreatsWildcardsLiterally(string marker)
    {
        // Arrange
        using var admin = await Administrator();
        var suffix = Guid.NewGuid().ToString("N");
        var request = NewEmployee() with { FirstName = $"Literal{marker}{suffix}" };
        using var created = await admin.PostAsJsonAsync("/api/users", request, TestContext.Current.CancellationToken);
        created.EnsureSuccessStatusCode();
        var expected = await created.Content.ReadFromJsonAsync<AccountDto>(TestContext.Current.CancellationToken);
        var other = NewEmployee() with { FirstName = $"LiteralX{suffix}" };
        using var otherCreated = await admin.PostAsJsonAsync("/api/users", other, TestContext.Current.CancellationToken);
        otherCreated.EnsureSuccessStatusCode();
        var search = Uri.EscapeDataString($" literal{marker}{suffix} ");

        // Act
        using var response = await admin.GetAsync($"/api/users?search={search}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedDto<AccountDto>>(TestContext.Current.CancellationToken);
        Assert.NotNull(page);
        Assert.Equal(expected, Assert.Single(page.Items));
        Assert.Equal(1, page.TotalItems);
    }

    [Fact]
    public async Task Users_MultipleAdministrators_CanManageAccounts()
    {
        // Arrange
        using var first = await Administrator();
        var request = NewEmployee();
        await CreateAdministrator(request);
        using var second = WithToken((await Login(request)).Token);

        // Act
        using var firstResponse = await first.GetAsync("/api/users", TestContext.Current.CancellationToken);
        using var secondResponse = await second.GetAsync("/api/users", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Refresh_MissingCookie_ReturnsUnauthorizedProblemDetails()
    {
        // Arrange
        using var client = fixture.CreateClient(authenticated: false);

        // Act
        using var response = await client.PostAsync("/api/auth/refresh", null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("Session expired.", problem.GetProperty("detail").GetString());
    }

    [Theory]
    [InlineData("cross-site")]
    [InlineData("same-origin")]
    public async Task CookieEndpoint_BrowserWithoutOrigin_ReturnsForbiddenProblemDetails(string fetchSite)
    {
        // Arrange
        using var client = fixture.CreateClient(authenticated: false);
        client.DefaultRequestHeaders.Add("Sec-Fetch-Site", fetchSite);

        // Act
        using var response = await client.PostAsync("/api/auth/refresh", null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(403, problem.GetProperty("status").GetInt32());
        Assert.True(problem.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task CookieEndpoint_UntrustedOrigin_IsRejected()
    {
        // Arrange
        var session = await Login(await CreateEmployee());
        using var client = WithCookie(session.Cookie);
        client.DefaultRequestHeaders.Add("Origin", "https://untrusted.example");

        // Act
        using var response = await client.PostAsync("/api/auth/refresh", null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("signature")]
    [InlineData("expired")]
    public async Task Jwt_InvalidToken_ReturnsUnauthorized(string defect)
    {
        // Arrange
        var session = await Login(await CreateEmployee());
        var original = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(session.Token);
        var settings = fixture.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<JwtOptions>>().Value;
        var key = defect is "signature" ? Convert.ToHexString(RandomNumberGenerator.GetBytes(32)) : settings.SigningKey;
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            defect is "issuer" ? "wrong" : settings.Issuer, defect is "audience" ? "wrong" : settings.Audience,
            original.Claims.Where(x => x.Type is not ("iss" or "aud" or "exp" or "nbf")), DateTime.UtcNow.AddHours(-1),
            defect is "expired" ? DateTime.UtcNow.AddMinutes(-1) : DateTime.UtcNow.AddMinutes(5),
            new Microsoft.IdentityModel.Tokens.SigningCredentials(new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256));
        using var client = WithToken(new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token));

        // Act
        using var response = await client.GetAsync("/api/profile", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Session_AbsoluteExpiry_RejectsRefreshAndAccess()
    {
        // Arrange
        var session = await Login(await CreateEmployee());
        var parsed = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(session.Token);
        var id = Guid.Parse(parsed.Claims.Single(x => x.Type is "sid").Value);
        await using var db = fixture.CreateDbContext();
        await db.AuthSessions.Where(x => x.Id == id).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ExpiresAt, DateTime.UtcNow.AddDays(-1)), TestContext.Current.CancellationToken);
        using var client = WithCookie(session.Cookie);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);

        // Act
        using var refresh = await client.PostAsync("/api/auth/refresh", null, TestContext.Current.CancellationToken);
        using var profile = await client.GetAsync("/api/profile", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, profile.StatusCode);
    }

    [Fact]
    public async Task CreateEmployee_InvalidPassword_LeavesNoAccountOrProfile()
    {
        // Arrange
        using var admin = await Administrator();
        var request = NewEmployee() with { Password = "longbutonlylowercase" };

        // Act
        using var response = await admin.PostAsJsonAsync("/api/users", request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var db = fixture.CreateDbContext();
        Assert.False(await db.Users.AnyAsync(x => x.Email == request.Email, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("Abc123!x", HttpStatusCode.Created)]
    [InlineData("Abc12!x", HttpStatusCode.BadRequest)]
    public async Task CreateEmployee_PasswordLength_RequiresAtLeastEightCharacters(string password, HttpStatusCode expectedStatus)
    {
        // Arrange
        using var admin = await Administrator();
        var request = NewEmployee() with { Password = password };

        // Act
        using var response = await admin.PostAsJsonAsync("/api/users", request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expectedStatus, response.StatusCode);
        if (expectedStatus == HttpStatusCode.Created)
            Assert.NotEmpty((await Login(request)).Token);
    }

    private async Task<CreateEmployeeCommand> CreateEmployee()
    {
        using var admin = await Administrator();
        var request = NewEmployee();
        using var response = await admin.PostAsJsonAsync("/api/users", request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return request;
    }

    private async Task<HttpClient> Administrator()
    {
        var request = new CreateEmployeeCommand("admin@example.test", "Office", "Admin", "Admin-test-password-123!");
        await CreateAdministrator(request);
        var session = await Login(request);
        return WithToken(session.Token);
    }

    private async Task CreateAdministrator(CreateEmployeeCommand request)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        if (await users.FindByEmailAsync(request.Email) is not null)
            return;
        await scope.ServiceProvider.GetRequiredService<IAccountService>().CreateEmployee(request, TestContext.Current.CancellationToken);
        var account = await users.FindByEmailAsync(request.Email);
        Assert.NotNull(account);
        Assert.True((await users.AddToRoleAsync(account, "Administrator")).Succeeded);
    }

    private async Task<(string Token, string Cookie)> Login(CreateEmployeeCommand request)
    {
        using var client = fixture.CreateClient(authenticated: false);
        using var response = await client.PostAsJsonAsync("/api/auth/login", request, TestContext.Current.CancellationToken);
        return await ReadSession(response);
    }

    private static async Task<(string Token, string Cookie)> ReadSession(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.False(body.TryGetProperty("refreshToken", out _));
        var header = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.Contains("httponly", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", header, StringComparison.OrdinalIgnoreCase);
        return (body.GetProperty("accessToken").GetString()!, header.Split(';')[0]);
    }

    private HttpClient WithToken(string token)
    {
        var client = fixture.CreateClient(authenticated: false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private HttpClient WithCookie(string cookie)
    {
        var client = fixture.CreateClient(authenticated: false);
        client.DefaultRequestHeaders.Add("Cookie", cookie);
        return client;
    }

    private static CreateEmployeeCommand NewEmployee() => new($"employee-{Guid.NewGuid():N}@example.test", "New", "Employee", "Employee-test-password-123!");
}
