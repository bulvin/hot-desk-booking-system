using System.Net.Http.Json;
using System.Text.Json;
using Infrastructure.Authentication;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Infrastructure.Health;

internal sealed class KeycloakReadinessCheck(HttpClient client, IOptions<KeycloakOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var discovery = await client.GetFromJsonAsync<JsonElement>(
            $"{options.Value.ServerAuthority}/.well-known/openid-configuration", cancellationToken);
        return discovery.ValueKind == JsonValueKind.Object
            && discovery.TryGetProperty("issuer", out var issuer)
            && issuer.ValueKind == JsonValueKind.String
            && string.Equals(issuer.GetString(), options.Value.Authority.TrimEnd('/'), StringComparison.Ordinal)
            ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy();
    }
}
