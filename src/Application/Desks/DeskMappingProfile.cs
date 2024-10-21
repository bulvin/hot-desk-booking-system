using Application.Dtos;
using AutoMapper;
using Domain.Desks;

namespace Application.Desks;

public class DeskMappingProfile : Profile
{
    public DeskMappingProfile()
    {
        CreateMap<Desk, DeskDto>();
    }
}