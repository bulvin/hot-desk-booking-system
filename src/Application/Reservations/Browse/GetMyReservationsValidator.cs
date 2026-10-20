using Application.Browsing;
using FluentValidation;

namespace Application.Reservations.Browse;

public sealed class GetMyReservationsValidator : AbstractValidator<GetMyReservationsQuery>
{
    public GetMyReservationsValidator() => RuleFor(x => x.Pagination).SetValidator(new PageRequestValidator());
}
