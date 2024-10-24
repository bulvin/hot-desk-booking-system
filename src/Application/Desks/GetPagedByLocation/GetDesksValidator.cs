using FluentValidation;

namespace Application.Desks.GetPagedByLocation;

public class GetDesksValidator : AbstractValidator<GetDesksByLocationQuery>
{
    public GetDesksValidator()
    {
        RuleFor(d => d.LocationId)
            .NotEmpty().WithMessage("Location is required");
        
        RuleFor(d => d.PaginationFilter.Page).GreaterThan(0)
            .WithMessage("Page must be greater than 0");
        
        RuleFor(d => d.PaginationFilter.PageSize)
            .LessThanOrEqualTo(30)
            .WithMessage("Page size must be less than or equal to 30");
    }
}