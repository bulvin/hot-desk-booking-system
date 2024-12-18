using Application.Dtos;
using Application.Interfaces.CQRS;
using AutoMapper;
using Domain;
using Domain.Desks;
using Domain.Exceptions;
using Domain.Exceptions.Desks;
using Domain.Reservations;
using Microsoft.AspNetCore.Http;

namespace Application.Reservations.BookDesk;

public record BookDeskCommand(Guid DeskId, DateOnly StartDate, DateOnly EndDate) : ICommand<ReservationDto>;

public class BookDeskHandler : ICommandHandler<BookDeskCommand, ReservationDto>
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IDeskRepository _deskRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BookDeskHandler(IReservationRepository repository, IUnitOfWork unitOfWork, IDeskRepository deskRepository, IMapper mapper, IHttpContextAccessor httpContextAccessor)
    {
        _reservationRepository = repository;
        _unitOfWork = unitOfWork;
        _deskRepository = deskRepository;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ReservationDto> Handle(BookDeskCommand command, CancellationToken cancellationToken)
    {
        var desk = await _deskRepository.GetById(command.DeskId, cancellationToken)
                   ?? throw new DeskNotFoundException(command.DeskId);

        if (!desk.IsAvailable)
            throw new DeskNotAvailableException(command.DeskId);
        
        var existingReservation = await _reservationRepository.HasActiveReservationForDesk(
                desk.Id, 
                command.StartDate, 
                command.EndDate,
                cancellationToken);

        if (existingReservation)
            throw new DeskAlreadyReservedException(command.DeskId, command.StartDate, command.EndDate);
        
        
        var reservation = new Reservation
        {
            DeskId = desk.Id,
            UserId = _httpContextAccessor.GetUserId(),
            StartDate = command.StartDate,
            EndDate = command.EndDate,
            Status = Status.Reserved
        };
        
        _reservationRepository.Add(reservation);
        await _unitOfWork.SaveChanges(cancellationToken);
        
        var reservationDto = _mapper.Map<ReservationDto>(reservation);
        return reservationDto;
    }
}