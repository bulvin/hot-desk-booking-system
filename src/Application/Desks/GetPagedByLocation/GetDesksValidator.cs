using FluentValidation;

namespace Application.Desks.GetPagedByLocation;

public class GetDesksValidator : AbstractValidator<GetDesksByLocationQuery>
{
    public GetDesksValidator()
    {
        RuleFor(d => d.LocationId)
            .NotEmpty().WithMessage("Location id is required");

        RuleFor(d => d.Page).GreaterThan(0);
        RuleFor(d => d.PageSize)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0")
            .LessThanOrEqualTo(30)
            .WithMessage("Page size must be less than or equal to 30");
    }
}