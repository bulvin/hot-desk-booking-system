using Application.Dtos;
using AutoMapper;
using Domain.Reservations;

namespace Application.Reservations;

public class ReservationMappingProfile : Profile
{
    public ReservationMappingProfile()
    {
        CreateMap<Reservation, ReservationDto>();
        CreateMap<ReservationDto, Reservation>();
    }
}