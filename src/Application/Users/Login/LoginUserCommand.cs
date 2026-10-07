using Application.Interfaces.CQRS;

namespace Application.Users.Login;

public record LoginUserCommand(string Email, string Password) : ICommand<string>;