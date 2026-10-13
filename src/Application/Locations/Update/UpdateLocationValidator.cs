using FluentValidation;

namespace Application.Locations.Update;

public class UpdateLocationValidator : AbstractValidator<UpdateLocationCommand>
{
    public UpdateLocationValidator()
    {
        RuleFor(command => command.Name).NotEmpty();
        RuleFor(command => command.Address)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .DependentRules(() =>
            {
                RuleFor(command => command.Address.Street).NotEmpty().MaximumLength(120);
                RuleFor(command => command.Address.BuildingNumber).NotEmpty().MaximumLength(16);
                RuleFor(command => command.Address.City).NotEmpty().MaximumLength(100);
                RuleFor(command => command.Address.PostalCode).NotEmpty().MaximumLength(16);
            });
    }
}
