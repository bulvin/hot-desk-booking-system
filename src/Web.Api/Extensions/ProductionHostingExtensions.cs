using System.Net;
using Infrastructure.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Web.Api.Infrastructure;

namespace Web.Api.Extensions;

internal static class ProductionHostingExtensions
{
    internal static void AddProductionHosting(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.IsProduction())
            ValidateProductionConfiguration(configuration);

        services.AddOptions<ProxyOptions>().Bind(configuration.GetSection("ReverseProxy"))
            .Validate(options => options.ForwardLimit > 0, "ReverseProxy:ForwardLimit must be positive.")
            .Validate(options => options.KnownProxies.All(value => IPAddress.TryParse(value, out _)),
                "ReverseProxy:KnownProxies must contain valid IP addresses.")
            .Validate(options => options.KnownNetworks.All(value => System.Net.IPNetwork.TryParse(value, out var network) && network.PrefixLength > 0),
                "ReverseProxy:KnownNetworks must contain valid CIDRs narrower than /0.")
            .Validate(options => !environment.IsProduction() || options.KnownProxies.Length + options.KnownNetworks.Length > 0,
                "Production requires explicitly trusted reverse proxy addresses or networks.")
            .ValidateOnStart();
        services.AddOptions<ForwardedHeadersOptions>().Configure<IOptions<ProxyOptions>>((options, settings) =>
        {
            var proxy = settings.Value;
            // No configured proxy means no forwarded headers, including in local development.
            options.ForwardedHeaders = proxy.KnownProxies.Length + proxy.KnownNetworks.Length == 0
                ? ForwardedHeaders.None : ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = proxy.ForwardLimit;
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();
            foreach (var address in proxy.KnownProxies)
                options.KnownProxies.Add(IPAddress.Parse(address));
            foreach (var network in proxy.KnownNetworks)
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
        });


        services.AddHostedService<DatabaseInitializationHostedService>();
        services.AddDependencyHealthChecks();
    }

    internal static void UsePrivateHealthChecks(this IApplicationBuilder app)
    {
        var port = app.ApplicationServices.GetRequiredService<IConfiguration>().GetValue<int?>("HealthChecks:Port");
        if (port.HasValue)
            app.Use(async (context, next) =>
            {
                var isHealth = context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase);
                if (isHealth != (context.Connection.LocalPort == port.Value))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }
                await next(context);
            });
        app.UseHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.UseHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
    }

    private static void ValidateProductionConfiguration(IConfiguration configuration)
    {
        if (configuration.GetValue<bool>("ForwardedHeaders_Enabled"))
            throw new InvalidOperationException("Use explicit ReverseProxy trust settings instead of blanket forwarded headers in Production.");
        var hosts = configuration["AllowedHosts"]?.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (hosts is not { Length: > 0 } || hosts.Any(host => host.Contains('*', StringComparison.Ordinal)
                || Uri.CheckHostName(host) == UriHostNameType.Unknown))
            throw new InvalidOperationException("Production requires explicit AllowedHosts hostnames without wildcards or ports.");
        if (!IsHttps(configuration["Keycloak:Authority"])
            || configuration["Keycloak:BackchannelAuthority"] is { } backchannel && !IsHttps(backchannel)
            || !configuration.GetValue("Keycloak:RequireHttpsMetadata", true))
            throw new InvalidOperationException("Production requires HTTPS Keycloak authority, backchannel and metadata.");
        if (configuration.GetValue<bool>("Database:SeedSampleData"))
            throw new InvalidOperationException("Sample data seeding is forbidden in Production.");
    }

    private static bool IsHttps(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal);
}
