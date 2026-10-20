using Application.Browsing;
using FluentValidation;

namespace Application.Locations.Browse;

public sealed class GetLocationsValidator : AbstractValidator<GetLocationsQuery>
{
    public GetLocationsValidator() => RuleFor(x => x.Pagination).SetValidator(new PageRequestValidator());
}
