using FluentValidation;

namespace Application.Users.Register;

public class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(c => c.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(c => c.LastName).NotEmpty().MaximumLength(50);
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(100);
        RuleFor(c => c.Password).NotEmpty().MinimumLength(8);
    }
}