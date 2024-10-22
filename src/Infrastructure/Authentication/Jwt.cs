using Domain.Users;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Authentication;
public class JwtOptions
{
    public required string Key { get; init; }
    public required int Expires { get; init; }
}

public class Jwt : ITokenProvider
{
    private readonly JwtOptions _options;

    public Jwt(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateToken(User user)
    {
        var key = SecurityKey(_options.Key);

        var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email)
        };
        
        var roles = user.Roles;
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role.Name.ToString())));
        
        var token = new JwtSecurityToken
        (
            claims: claims,
            signingCredentials: signingCredentials,
            expires: DateTime.UtcNow.AddDays(_options.Expires)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static SymmetricSecurityKey SecurityKey(string key) => new(Encoding.ASCII.GetBytes(key));
}
