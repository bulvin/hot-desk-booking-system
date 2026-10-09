namespace Web.Api.Requests;

internal sealed record RegisterUserRequest(string Email, string FirstName, string LastName, string Password);
