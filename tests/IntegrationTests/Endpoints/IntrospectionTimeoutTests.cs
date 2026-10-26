using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IntegrationTests.Endpoints;

public sealed class IntrospectionTimeoutTests
{
    [Fact]
    public async Task Authentication_IntrospectionTimesOut_FailsWithoutSynchronizingUser()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["ConnectionStrings:Database"] = "Host=localhost;Database=unused;Username=unused;Password=unused",
            ["Keycloak:Authority"] = "https://identity.example.test/realms/hotdesk",
            ["Keycloak:ClientSecret"] = "test-only",
            ["Keycloak:IntrospectionTimeoutSeconds"] = "1"
        }).Build();
        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        services.ConfigureHttpClientDefaults(client => client.ConfigurePrimaryHttpMessageHandler(() => new UnresponsiveHandler()));
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var options = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);
        var events = (JwtBearerEvents)scope.ServiceProvider.GetRequiredService(options.EventsType!);
        var context = new TokenValidatedContext(new DefaultHttpContext { RequestServices = scope.ServiceProvider },
            new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler)), options)
        {
            Principal = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim("sub", Guid.NewGuid().ToString()), new Claim("email_verified", "true")], "test")),
            SecurityToken = new JwtSecurityTokenHandler().ReadJwtToken(new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken()))
        };

        // Act
        await events.TokenValidated(context).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(context.Result?.Failure);
        Assert.Equal("Token introspection unavailable.", context.Result.Failure.Message);
        Assert.False(context.Result.Succeeded);
        Assert.Null(context.Principal.FindFirst(ClaimTypes.NameIdentifier));
    }

    private sealed class UnresponsiveHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        }
    }
}
