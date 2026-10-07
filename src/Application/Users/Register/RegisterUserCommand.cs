using Application.Interfaces.CQRS;

namespace Application.Users.Register;

public record RegisterUserCommand(string Email, string FirstName, string LastName, string Password)
    : ICommand<Guid>;