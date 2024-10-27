using Application.Interfaces.CQRS;
using Domain;
using Domain.Desks;
using Domain.Exceptions;
using Domain.Exceptions.Desks;
using Domain.Exceptions.Reservations;
using Domain.Reservations;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Reservations.ChangeDesk;

public record ChangeDeskCommand(Guid Id, Guid DeskId) : ICommand<Unit>;
    
public class ChangeDeskHandler : ICommandHandler<ChangeDeskCommand, Unit>
{
    private readonly IReservationRepository _repository;
    private readonly IDeskRepository _deskRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ChangeDeskHandler(IReservationRepository repository, IUnitOfWork unitOfWork, IDeskRepository deskRepository, IHttpContextAccessor httpContextAccessor)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _deskRepository = deskRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Unit> Handle(ChangeDeskCommand command, CancellationToken cancellationToken)
    {
        var reservation = await _repository.GetById(command.Id, cancellationToken)
            ?? throw new ReservationNotFoundException(command.Id);

        if (reservation.DeskId == command.DeskId)
            throw new SameDeskChangeException(command.DeskId);

        if (reservation.UserId != _httpContextAccessor.GetUserId())
            throw new UnauthorizedReservationChangeException(command.Id, _httpContextAccessor.GetUserId());

        if (!await _deskRepository.Exists(command.DeskId, cancellationToken))
            throw new DeskNotFoundException(command.DeskId);
        
        if (reservation.StartDate < DateOnly.FromDateTime(DateTime.Now.AddHours(24)))
        {
            throw new TooLateForReservationChangeException(command.Id, reservation.StartDate);
        }

        reservation.DeskId = command.DeskId;
        await _unitOfWork.SaveChanges(cancellationToken);
        
        return Unit.Value;
    }
}