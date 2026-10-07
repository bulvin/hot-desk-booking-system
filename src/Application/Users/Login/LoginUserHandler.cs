using Application.Interfaces;
using Application.Interfaces.CQRS;
using Domain.Exceptions.Users;
using Domain.Users;

namespace Application.Users.Login;

public class LoginUserHandler(IUserRepository repository, IPasswordHasher passwordHasher, ITokenProvider tokenProvider)
    : ICommandHandler<LoginUserCommand, string>
{
    public async Task<string> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var user = await repository.GetByEmail(request.Email, cancellationToken);
        if (user == null)
            throw new InvalidCredentialsException();

        var verified = passwordHasher.Verify(request.Password, user.Password);
        if (!verified)
            throw new InvalidCredentialsException();

        var token = tokenProvider.GenerateToken(user);
        return token;
    }
}