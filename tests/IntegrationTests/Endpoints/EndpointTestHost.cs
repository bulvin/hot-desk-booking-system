using System.Security.Claims;
using Application.Interfaces;
using Infrastructure.Authentication;
using Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Web.Api.Endpoints;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace IntegrationTests.Endpoints;

internal sealed class EndpointTestHost : IAsyncLifetime
{
    public Mock<ISender> Sender { get; } = new(MockBehavior.Strict);

    private readonly WebApplication _app;

    public IServiceProvider Services => _app.Services;
    public HttpClient Client { get; private set; } = null!;

    public EndpointTestHost()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Production });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton(Sender.Object);

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IUserContext>(_ => new Mock<IUserContext>(MockBehavior.Strict).Object);

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(PolicyNames.Admin, policy => policy.RequireClaim(ClaimTypes.Role, PolicyNames.Admin));
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = ProblemDetailsConfiguration.Customize);
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGenWithAuth(builder.Configuration);

        _app = builder.Build();
        _app.UseExceptionHandler();
        _app.UseStatusCodePages();
        _app.UseAuthentication();
        _app.Use(async (context, next) =>
        {
            if (context.Request.Headers.TryGetValue("Test-Role", out var role))
            {
                context.User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.Role, role.ToString())], "Test"));
            }

            await next(context);
        });
        _app.UseAuthorization();

        _app.MapAuthEndpoints();
        _app.MapLocationEndpoints();
        _app.MapDeskEndpoints();
        _app.MapReservationEndpoints();
    }

    public async ValueTask InitializeAsync()
    {
        await _app.StartAsync(TestContext.Current.CancellationToken);
        Client = _app.GetTestClient();
        Client.DefaultRequestHeaders.Add("Test-Role", PolicyNames.Admin);
    }

    public async ValueTask DisposeAsync()
    {
        if (Client is not null)
            Client.Dispose();
        await _app.DisposeAsync();
    }
}
