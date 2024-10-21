using FluentValidation;

namespace Application.Reservations.BookDesk;

public class BookDeskCommandValidator : AbstractValidator<BookDeskCommand>
{
    public BookDeskCommandValidator()
    {
        RuleFor(b => b.StartDate)
            .Must((b, startDate) => startDate <= b.EndDate)
            .WithMessage("Start date must be before or equal to end date.");

        RuleFor(b => b.StartDate)
            .Must(startDate => startDate >= DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Cannot book a desk for past dates.");

        RuleFor(command => command)
            .Must(command => (command.EndDate.ToDateTime(TimeOnly.MinValue) - command.StartDate.ToDateTime(TimeOnly.MinValue)).Days + 1 <= 7)
            .WithMessage("Booking duration cannot exceed 7 days.");
    }
}