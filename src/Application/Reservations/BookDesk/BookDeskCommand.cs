using Application.Dtos;
using Application.Interfaces.CQRS;

namespace Application.Reservations.BookDesk;

public record BookDeskCommand(Guid DeskId, DateOnly StartDate, DateOnly EndDate) : ICommand<ReservationDto>;