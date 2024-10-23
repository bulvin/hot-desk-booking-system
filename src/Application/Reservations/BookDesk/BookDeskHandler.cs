using Application.Dtos;
using Application.Interfaces.CQRS;
using AutoMapper;
using Domain;
using Domain.Desks;
using Domain.Reservations;
using Infrastructure.Authentication;
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
                   ?? throw new ApplicationException($"Desk with {command.DeskId} not found");
        
        var existingReservation = await _reservationRepository.HasActiveReservationForDesk(
                desk.Id, 
                command.StartDate, 
                command.EndDate,
                cancellationToken);

        if (existingReservation)
            throw new ApplicationException("The desk is already booked for the specified period");
        
        var reservation = new Reservation
        {
            DeskId = desk.Id,
            UserId = _httpContextAccessor.GetUserId(),
            StartDate = command.StartDate,
            EndDate = command.EndDate,
            Status = Status.Active
        };
        
        _reservationRepository.Add(reservation);
        desk.IsAvailable = false;
        await _unitOfWork.SaveChanges(cancellationToken);
        
        var reservationDto = _mapper.Map<ReservationDto>(reservation);
        return reservationDto;
    }
}