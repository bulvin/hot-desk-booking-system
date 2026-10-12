using Application.Dtos;

namespace Application.Interfaces;

public interface IUserContext
{
    ProfileDto GetCurrentUser();
}
