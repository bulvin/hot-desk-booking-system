namespace Application.Accounts;

public sealed record SessionDto(string AccessToken, DateTime ExpiresAt, string RefreshToken, DateTime SessionExpiresAt);
