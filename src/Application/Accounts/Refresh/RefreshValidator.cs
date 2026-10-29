using FluentValidation;

namespace Application.Accounts.Refresh;

public sealed class RefreshValidator : AbstractValidator<RefreshCommand>
{
    public RefreshValidator()
    {
        RuleFor(x => x.RefreshToken).MaximumLength(256);
    }
}
