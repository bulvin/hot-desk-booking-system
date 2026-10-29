namespace Web.Api.Infrastructure;

internal sealed class ApiRateLimitingOptions
{
    public int AuthPermitLimit { get; set; } = 30;
    public int IpPermitLimit { get; set; } = 1200;
    public int UserPermitLimit { get; set; } = 120;
    public int WritePermitLimit { get; set; } = 20;
    public int WindowSeconds { get; set; } = 60;
}
