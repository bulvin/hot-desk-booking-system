using Domain.Users;

namespace Application.Interfaces;

public interface ITokenProvider
{
    string GenerateToken(User user);
}