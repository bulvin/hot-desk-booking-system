using Application.Interfaces;
using FluentValidation;

namespace Application.Reservations.BookDesk;

public class BookDeskCommandValidator : AbstractValidator<BookDeskCommand>
{
    public BookDeskCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(b => b.StartDate)
            .Must((b, startDate) => startDate <= b.EndDate)
            .WithMessage("Start date must be before or equal to end date.");

        RuleFor(b => b.StartDate)
            .Custom((startDate, context) =>
            {
                var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);
                if (startDate < today)
                    context.AddFailure("Cannot book a desk for past dates.");
                if (startDate.DayNumber - today.DayNumber > 7)
                    context.AddFailure("You can only book a desk up to one week in advance.");
            });

        RuleFor(command => command)
            .Must(command => command.EndDate.DayNumber - command.StartDate.DayNumber + 1 <= 7)
            .WithMessage("Booking duration cannot exceed 7 days.");
    }
}
