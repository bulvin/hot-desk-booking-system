using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Interfaces;
using Domain.Users;
using Infrastructure;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace UnitTests.Authentication;

public class JwtTests
{
    private const string SigningKey = "unit-test-signing-key-with-at-least-32-bytes";

    [Theory]
    [InlineData(null, null, false)]
    [InlineData(null, null, true)]
    [InlineData("", "", false)]
    [InlineData(" ", " ", false)]
    public void StartupValidation_AcceptsDefaultIssuerAndAudience(string? issuer, string? audience, bool includeNullOverrides)
    {
        using var services = CreateServices(issuer, audience, includeNullOverrides);

        services.GetRequiredService<IStartupValidator>().Validate();

        var options = services.GetRequiredService<IOptions<JwtOptions>>().Value;
        Assert.Equal(JwtOptions.DefaultIssuer, options.Issuer);
        Assert.Equal(JwtOptions.DefaultAudience, options.Audience);
        Assert.Equal(options.Issuer, GetValidation(services).ValidIssuer);
        Assert.Equal(options.Audience, GetValidation(services).ValidAudience);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("test-issuer", "test-audience")]
    public void GenerateToken_MatchesConfiguredValidation(string? issuer, string? audience)
    {
        using var services = CreateServices(issuer, audience);
        var user = new User { Id = Guid.NewGuid(), Email = "test@example.com" };
        var token = services.GetRequiredService<ITokenProvider>().GenerateToken(user);
        var validation = GetValidation(services);

        var principal = new JwtSecurityTokenHandler().ValidateToken(token, validation, out var validatedToken);
        var jwt = Assert.IsType<JwtSecurityToken>(validatedToken);

        Assert.True(validation.ValidateIssuer);
        Assert.True(validation.ValidateAudience);
        Assert.Equal(issuer ?? JwtOptions.DefaultIssuer, jwt.Issuer);
        Assert.Contains(audience ?? JwtOptions.DefaultAudience, jwt.Audiences, StringComparer.Ordinal);
        Assert.Equal(user.Id.ToString(), principal.FindFirstValue(ClaimTypes.NameIdentifier));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ValidateToken_RejectsIncorrectIssuerOrAudience(bool incorrectIssuer)
    {
        using var services = CreateServices(issuer: null, audience: null);
        var generator = new JwtTokenProvider(Options.Create(new JwtOptions
        {
            Key = SigningKey,
            Expires = 1,
            Issuer = incorrectIssuer ? "other-issuer" : JwtOptions.DefaultIssuer,
            Audience = incorrectIssuer ? JwtOptions.DefaultAudience : "other-audience"
        }));
        var token = generator.GenerateToken(new User { Email = "test@example.com" });
        var validation = GetValidation(services);
        var handler = new JwtSecurityTokenHandler();

        if (incorrectIssuer)
            Assert.Throws<SecurityTokenInvalidIssuerException>(() => handler.ValidateToken(token, validation, out _));
        else
            Assert.Throws<SecurityTokenInvalidAudienceException>(() => handler.ValidateToken(token, validation, out _));
    }

    private static ServiceProvider CreateServices(string? issuer, string? audience, bool includeNullOverrides = false)
    {
        var settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ConnectionStrings:Database"] = "Host=localhost;Database=unit_tests;Username=unit_tests",
            ["Jwt:Key"] = SigningKey,
            ["Jwt:Expires"] = "1"
        };
        if (issuer is not null || includeNullOverrides)
            settings["Jwt:Issuer"] = issuer;
        if (audience is not null || includeNullOverrides)
            settings["Jwt:Audience"] = audience;

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        return new ServiceCollection().AddInfrastructure(configuration).BuildServiceProvider();
    }

    private static TokenValidationParameters GetValidation(IServiceProvider services)
    {
        return services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme).TokenValidationParameters;
    }

    [Theory]
    [InlineData("", "test-audience")]
    [InlineData(" ", "test-audience")]
    [InlineData("test-issuer", "")]
    [InlineData("test-issuer", " ")]
    public void ConfigureOptions_BlankIssuerOrAudienceUsesDefaults(string issuer, string audience)
    {
        using var services = CreateServices(issuer, audience);

        services.GetRequiredService<IStartupValidator>().Validate();
        var options = services.GetRequiredService<IOptions<JwtOptions>>().Value;
        Assert.Equal(string.IsNullOrWhiteSpace(issuer) ? JwtOptions.DefaultIssuer : issuer, options.Issuer);
        Assert.Equal(string.IsNullOrWhiteSpace(audience) ? JwtOptions.DefaultAudience : audience, options.Audience);

        var token = services.GetRequiredService<ITokenProvider>().GenerateToken(new User { Email = "test@example.com" });
        new JwtSecurityTokenHandler().ValidateToken(token, GetValidation(services), out _);
    }
}
