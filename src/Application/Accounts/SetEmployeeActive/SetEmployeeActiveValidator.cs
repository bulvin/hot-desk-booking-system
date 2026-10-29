using FluentValidation;

namespace Application.Accounts.SetEmployeeActive;

public sealed class SetEmployeeActiveValidator : AbstractValidator<SetEmployeeActiveCommand>
{
    public SetEmployeeActiveValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
