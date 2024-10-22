using FluentValidation;

namespace Application.Desks.Create;

public class CreateDeskValidator : AbstractValidator<CreateDeskCommand>
{
    public CreateDeskValidator()
    {
        RuleFor(d => d.Name)
            .NotEmpty().WithMessage("Desk name/number is required");

        RuleFor(d => d.LocationId)
            .NotEmpty().WithMessage("Location id is required");

        RuleFor(d => d.Name)
            .MaximumLength(50).WithMessage("Desk name can be at most 50 characters long");
    }
}