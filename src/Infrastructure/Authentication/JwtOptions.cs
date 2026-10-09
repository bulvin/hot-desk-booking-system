namespace Infrastructure.Authentication;

public class JwtOptions
{
    public const string DefaultIssuer = "hot-desk-booking-system";
    public const string DefaultAudience = "hot-desk-booking-system-api";

    public required string Key { get; init; }
    public required int Expires { get; init; }
    public string Issuer { get; set; } = DefaultIssuer;
    public string Audience { get; set; } = DefaultAudience;
}
