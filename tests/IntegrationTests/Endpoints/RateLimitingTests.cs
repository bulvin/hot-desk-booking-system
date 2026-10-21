using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace IntegrationTests.Endpoints;

public sealed class RateLimitingTests
{
    [Fact]
    public async Task UserLimit_SharedAcrossRoutesAndIndependentAcrossUsers()
    {
        // Arrange
        await using var host = await LimitHost.StartAsync(userLimit: 2);

        // Act
        using var first = await host.SendAsync("/api/read");
        using var second = await host.SendAsync("/api/other");
        using var rejected = await host.SendAsync("/api/read");
        using var otherUser = await host.SendAsync("/api/read", user: "other");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, otherUser.StatusCode);
        await AssertRejection(rejected, "GET /api/read");
    }

    [Theory]
    [InlineData("Employee")]
    [InlineData("Administrator")]
    public async Task WriteLimit_SharedAcrossMethodsAndRoutes_LeavesReadsAvailable(string role)
    {
        // Arrange
        await using var host = await LimitHost.StartAsync(writeLimit: 2);

        // Act
        using var first = await host.SendAsync("/api/write", method: "POST", role: role);
        using var second = await host.SendAsync("/api/other", method: "PUT", role: role);
        using var rejected = await host.SendAsync("/api/write", method: "DELETE", role: role);
        using var read = await host.SendAsync("/api/read", role: role);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, read.StatusCode);
    }

    [Fact]
    public async Task Writes_AlsoConsumeTotalUserAllowance()
    {
        // Arrange
        await using var host = await LimitHost.StartAsync(userLimit: 2);

        // Act
        using var write = await host.SendAsync("/api/write", method: "PATCH");
        using var read = await host.SendAsync("/api/read");
        using var rejected = await host.SendAsync("/api/read");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, write.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, read.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("192.0.2.1")]
    public async Task IpLimit_RejectsBeforeAuthentication_AndIgnoresForwardedHeaders(string? ip)
    {
        // Arrange
        await using var host = await LimitHost.StartAsync(ipLimit: 1);

        // Act
        using var first = await host.SendAsync("/api/missing", user: null, ip: ip);
        using var rejected = await host.SendAsync("/api/profile", user: null, ip: ip, forwarded: "198.51.100.1");
        using var differentIp = await host.SendAsync("/api/profile", user: null, ip: "192.0.2.2");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, first.StatusCode);
        await AssertRejection(rejected, "GET /api/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, differentIp.StatusCode);
        Assert.Equal(2, host.AuthenticationCalls);
    }

    [Fact]
    public async Task IpLimit_NormalizesMappedIpv4Addresses()
    {
        // Arrange
        await using var host = await LimitHost.StartAsync(ipLimit: 1);

        // Act
        using var first = await host.SendAsync("/api/read", ip: "192.0.2.1");
        using var rejected = await host.SendAsync("/api/read", ip: "::ffff:192.0.2.1");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ConcurrentRequests_DoNotExceedAllowance(bool ipLimit)
    {
        // Arrange
        await using var host = await LimitHost.StartAsync(ipLimit: ipLimit ? 3 : 100, userLimit: ipLimit ? 100 : 3);

        // Act
        var responses = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => host.SendAsync("/api/read")));

        // Assert
        try
        {
            Assert.Equal(3, responses.Count(response => response.StatusCode == HttpStatusCode.NoContent));
            Assert.Equal(17, responses.Count(response => response.StatusCode == HttpStatusCode.TooManyRequests));
        }
        finally
        {
            foreach (var response in responses)
                response.Dispose();
        }
    }

    [Fact]
    public async Task WindowRenewal_AllowsRequestsAgain()
    {
        // Arrange
        await using var host = await LimitHost.StartAsync(ipLimit: 1, userLimit: 1, writeLimit: 1, windowSeconds: 1);
        using var first = await host.SendAsync("/api/write", method: "POST");
        using var rejected = await host.SendAsync("/api/write", method: "POST");

        // Act
        await Task.Delay(TimeSpan.FromMilliseconds(1500), TestContext.Current.CancellationToken);
        using var renewed = await host.SendAsync("/api/write", method: "POST");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, renewed.StatusCode);
    }

    [Fact]
    public async Task NonApiPaths_DoNotConsumeAllowances()
    {
        // Arrange
        await using var host = await LimitHost.StartAsync(ipLimit: 1, userLimit: 1);

        // Act
        using var swagger = await host.SendAsync("/swagger");
        using var unrelated = await host.SendAsync("/api-other");
        using var api = await host.SendAsync("/api/read");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, swagger.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unrelated.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, api.StatusCode);
    }

    [Theory]
    [InlineData("IpPermitLimit", 0)]
    [InlineData("UserPermitLimit", -1)]
    [InlineData("WritePermitLimit", 0)]
    [InlineData("WindowSeconds", 0)]
    public async Task InvalidConfiguration_FailsStartup(string key, int value)
    {
        // Arrange
        var configuration = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [$"RateLimiting:{key}"] = value.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };

        // Act
        var exception = await Record.ExceptionAsync(async () =>
        {
            await using var host = await LimitHost.StartAsync(overrides: configuration);
        });

        // Assert
        Assert.IsType<OptionsValidationException>(exception);
    }

    private static async Task AssertRejection(HttpResponseMessage response, string instance)
    {
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.True(response.Headers.RetryAfter?.Delta > TimeSpan.Zero);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var problem = document.RootElement;
        Assert.Equal(429, problem.GetProperty("status").GetInt32());
        Assert.Equal("https://www.rfc-editor.org/rfc/rfc6585#section-4", problem.GetProperty("type").GetString());
        Assert.Equal(instance, problem.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrEmpty(problem.GetProperty("traceId").GetString()));
        Assert.True(problem.TryGetProperty("timestamp", out _));
    }

    private sealed class LimitHost(WebApplication app) : IAsyncDisposable
    {
        private HttpClient _client = null!;
        private int _authenticationCalls;
        public int AuthenticationCalls => _authenticationCalls;

        public static async Task<LimitHost> StartAsync(int ipLimit = 100, int userLimit = 100, int writeLimit = 100,
            int windowSeconds = 60, Dictionary<string, string?>? overrides = null)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
            builder.Configuration.AddInMemoryCollection(overrides ?? new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["RateLimiting:IpPermitLimit"] = ipLimit.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["RateLimiting:UserPermitLimit"] = userLimit.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["RateLimiting:WritePermitLimit"] = writeLimit.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["RateLimiting:WindowSeconds"] = windowSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)
            });
            builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = ProblemDetailsConfiguration.Customize);
            builder.Services.AddApiRateLimiting(builder.Configuration);
            var app = builder.Build();
            var host = new LimitHost(app);
            app.UseStatusCodePages();
            // These test-only headers simulate the connection and authenticated principal.
            app.Use(async (context, next) =>
            {
                context.Connection.RemoteIpAddress = context.Request.Headers.TryGetValue("Test-IP", out var ip)
                    ? IPAddress.Parse(ip.ToString()) : null;
                await next(context);
            });
            app.UseApiIpRateLimiting();
            app.Use(async (context, next) =>
            {
                Interlocked.Increment(ref host._authenticationCalls);
                if (context.Request.Headers.TryGetValue("Test-User", out var user))
                    context.User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, user.ToString()),
                            new Claim(ClaimTypes.Role, context.Request.Headers["Test-Role"].ToString())], "Test"));
                await next(context);
            });
            app.UseRateLimiter();
            app.MapGet("/api/profile", (HttpContext context) =>
                context.User.Identity?.IsAuthenticated == true ? Results.NoContent() : Results.Unauthorized());
            app.MapMethods("/api/{action:regex(^(read|write|other)$)}", ["GET", "POST", "PUT", "PATCH", "DELETE"], () => Results.NoContent());
            try
            {
                await app.StartAsync(TestContext.Current.CancellationToken);
                host._client = app.GetTestClient();
                return host;
            }
            catch
            {
                await app.DisposeAsync();
                throw;
            }
        }

        public async Task<HttpResponseMessage> SendAsync(string path, string method = "GET", string? user = "user",
            string role = "Employee", string? ip = null, string? forwarded = null)
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), path);
            if (user is not null)
                request.Headers.Add("Test-User", user);
            request.Headers.Add("Test-Role", role);
            if (ip is not null)
                request.Headers.Add("Test-IP", ip);
            if (forwarded is not null)
                request.Headers.Add("X-Forwarded-For", forwarded);
            return await _client.SendAsync(request, TestContext.Current.CancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            _client.Dispose();
            await app.DisposeAsync();
        }
    }
}
