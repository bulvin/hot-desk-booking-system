using Application.Interfaces;
using Application.Interfaces.CQRS;
using Domain.Users;

namespace Application.Users.Login;

public record LoginUserCommand(string Email, string Password) : ICommand<string>;

public class LoginUserHandler : ICommandHandler<LoginUserCommand, string>
{
    private readonly IUserRepository _repository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenProvider _tokenProvider;

    public LoginUserHandler(IUserRepository repository, IPasswordHasher passwordHasher, ITokenProvider token)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _tokenProvider = token;
    }

    public async Task<string> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByEmail(request.Email, cancellationToken);
        if (user == null)
            throw new ApplicationException("Email or password are invalid");

        var verified = _passwordHasher.Verify(request.Password, user.Password);
        if (!verified)
            throw new ApplicationException("Email or password are invalid");

        var token = _tokenProvider.GenerateToken(user);
        return token;
    }
}