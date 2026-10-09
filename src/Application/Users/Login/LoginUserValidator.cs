using FluentValidation;

namespace Application.Users.Login;

public class LoginUserValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(100);

        RuleFor(command => command.Password).NotEmpty();
    }
}
