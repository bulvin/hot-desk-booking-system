namespace Infrastructure.Authentication;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "hotdesk";
    public string Audience { get; set; } = "hotdesk-api";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 10;
    public int SessionDays { get; set; } = 7;
}
