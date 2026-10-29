namespace Infrastructure.Authentication;

public sealed class RefreshCredential
{
    public string Hash { get; set; } = string.Empty;
    public Guid SessionId { get; set; }
    public bool IsUsed { get; set; }
}
