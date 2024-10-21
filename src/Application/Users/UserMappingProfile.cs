using Application.Dtos;
using AutoMapper;
using Domain.Users;

namespace Application.Users;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>();
    }
}