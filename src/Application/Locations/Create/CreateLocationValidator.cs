using FluentValidation;

namespace Application.Locations.Create;

public class CreateLocationValidator : AbstractValidator<CreateLocationCommand>
{
    public CreateLocationValidator()
    {
        RuleFor(l => l.Name)
            .NotEmpty().WithMessage("Location name is required");
        
        RuleFor(l => l.Address)
            .NotNull().WithMessage("Address is required.");
        
        RuleFor(l => l.Address.Street)
                .NotEmpty().WithMessage("Street is required.")
                .MaximumLength(120).WithMessage("Street can be at most 120 characters long.");
            
        RuleFor(l => l.Address.BuildingNumber)
                .NotEmpty().WithMessage("Building number is required.")
                .MaximumLength(16).WithMessage("Building number can be at most 16 characters long.");
            
        RuleFor(l => l.Address.City)
                .NotEmpty().WithMessage("City is required.")
                .MaximumLength(100).WithMessage("City can be at most 100 characters long.");
            
        RuleFor(l => l.Address.PostalCode)
                .NotEmpty().WithMessage("Postal code is required.")
                .MaximumLength(16).WithMessage("Postal code can be at most 16 characters long.");
    }
}