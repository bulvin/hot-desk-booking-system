namespace Infrastructure.Authentication;

public sealed class KeycloakOptions
{
    public string Authority { get; set; } = string.Empty;
    public string? BackchannelAuthority { get; set; }
    public string Audience { get; set; } = "hotdesk-api";
    public string ClientSecret { get; set; } = string.Empty;
    public string SwaggerClientId { get; set; } = "hotdesk-swagger";
    public bool RequireHttpsMetadata { get; set; } = true;

    public string ServerAuthority => (BackchannelAuthority ?? Authority).TrimEnd('/');
}
