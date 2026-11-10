using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RunWay.Api.Data;
using RunWay.Api.Dtos;

namespace RunWay.Api.Services;

public interface ITokenService
{
    AuthResponse CreateToken(ApplicationUser user, IEnumerable<string> roles);
}

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config) => _config = config;

    public AuthResponse CreateToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var minutes = int.TryParse(_config["Jwt:ExpiryMinutes"], out var m) ? m : 60;
        var expires = DateTime.UtcNow.AddMinutes(minutes);
        var roleList = roles.ToList();

        // Short claim names ("sub", "role", ...) are used on purpose; see Program.cs (MapInboundClaims = false).
        var claims = new List<Claim>
        {
            new("sub", user.Id),
            new("stamp", user.SecurityStamp ?? ""),
            new("email", user.Email ?? string.Empty),
            new("name", user.FullName)
        };
        claims.AddRange(roleList.Select(r => new Claim("role", r)));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _config["Jwt:Issuer"],
            Audience = _config["Jwt:Audience"],
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);

        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expires,
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            ProfileImage = user.ProfileImagePath,
            Roles = roleList
        };
    }
}
