using Application.Dtos;
using Application.Interfaces.CQRS;
using Domain;
using Domain.Desks;
using Domain.Exceptions.Desks;
using Domain.Reservations;
using Microsoft.AspNetCore.Http;

namespace Application.Reservations.BookDesk;

public class BookDeskHandler(
    IReservationRepository repository,
    IUnitOfWork unitOfWork,
    IDeskRepository deskRepository,
    IHttpContextAccessor httpContextAccessor)
    : ICommandHandler<BookDeskCommand, ReservationDto>
{
    public async ValueTask<ReservationDto> Handle(BookDeskCommand command, CancellationToken cancellationToken)
    {
        var desk = await deskRepository.GetById(command.DeskId, cancellationToken)
                   ?? throw new DeskNotFoundException(command.DeskId);

        if (!desk.IsAvailable)
            throw new DeskNotAvailableException(command.DeskId);

        var existingReservation = await repository.HasActiveReservationForDesk(
                desk.Id,
                command.StartDate,
                command.EndDate,
                cancellationToken);

        if (existingReservation)
            throw new DeskAlreadyReservedException(command.DeskId, command.StartDate, command.EndDate);


        var reservation = new Reservation
        {
            DeskId = desk.Id,
            UserId = httpContextAccessor.GetUserId(),
            StartDate = command.StartDate,
            EndDate = command.EndDate,
            Status = Status.Reserved
        };

        repository.Add(reservation);
        await unitOfWork.SaveChanges(cancellationToken);

        var reservationDto = new ReservationDto(
            reservation.Id, reservation.StartDate, reservation.EndDate, reservation.Status);
        return reservationDto;
    }
}