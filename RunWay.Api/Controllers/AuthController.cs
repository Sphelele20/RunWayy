using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RunWay.Api.Data;
using RunWay.Api.Dtos;
using RunWay.Api.Services;

namespace RunWay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly ITokenService _tokens;

    public AuthController(UserManager<ApplicationUser> users, ITokenService tokens)
    {
        _users = users;
        _tokens = tokens;
    }

    /// <summary>Registers a Participant or Organiser account and returns a JWT.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        // Admin can never be self-assigned.
        if (request.Role != Roles.Participant && request.Role != Roles.Organiser)
            return Problem(detail: "Role must be Participant or Organiser.", statusCode: 400);

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Gender = request.Gender
        };

        var created = await _users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
            return Problem(detail: string.Join(" ", created.Errors.Select(e => e.Description)), statusCode: 400);

        var assigned = await _users.AddToRoleAsync(user, request.Role);
        if (!assigned.Succeeded) { await _users.DeleteAsync(user); return Problem(detail: "Unable to assign the selected role.", statusCode: 400); }

        var roles = await _users.GetRolesAsync(user);
        return Ok(_tokens.CreateToken(user, roles));
    }

    /// <summary>Validates credentials and returns a JWT.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _users.FindByEmailAsync(request.Email);
        if (user == null || await _users.IsLockedOutAsync(user) || !await _users.CheckPasswordAsync(user, request.Password))
        {
            if (user != null) await _users.AccessFailedAsync(user);
            return Problem(detail: "Invalid email or password.", statusCode: 401);
        }

        await _users.ResetAccessFailedCountAsync(user);
        var roles = await _users.GetRolesAsync(user);
        return Ok(_tokens.CreateToken(user, roles));
    }
    [HttpPost("logout"), Authorize]
    public async Task<IActionResult> Logout() {
        var user = await _users.FindByIdAsync(User.GetUserId()!);
        if (user != null) await _users.UpdateSecurityStampAsync(user);
        return NoContent();
    }
}
