using Application.Dtos;
using AutoMapper;
using Domain.Locations;

namespace Application.Locations;

public class LocationMappingProfile : Profile
{
    public LocationMappingProfile()
    {
        CreateMap<Location, LocationDto>();
        
        CreateMap<Address, AddressDto>();
    }
}