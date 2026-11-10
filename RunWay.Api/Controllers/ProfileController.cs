using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RunWay.Api.Data;
using RunWay.Api.Dtos;
using RunWay.Api.Services;

namespace RunWay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly IFileStorageService _files;

    public ProfileController(UserManager<ApplicationUser> users, IFileStorageService files)
    {
        _users = users;
        _files = files;
    }

    private async Task<ProfileDto> ToDto(ApplicationUser u) => new()
    {
        Id = u.Id,
        Email = u.Email ?? string.Empty,
        FirstName = u.FirstName,
        LastName = u.LastName,
        Gender = u.Gender,
        PhoneNumber = u.PhoneNumber,
        ProfileImage = u.ProfileImagePath,
        Roles = (await _users.GetRolesAsync(u)).ToList()
    };

    [HttpGet]
    public async Task<ActionResult<ProfileDto>> Get()
    {
        var user = await _users.FindByIdAsync(User.GetUserId()!);
        return user == null ? NotFound() : Ok(await ToDto(user));
    }

    [HttpPut]
    public async Task<ActionResult<ProfileDto>> Update(UpdateProfileRequest request)
    {
        var user = await _users.FindByIdAsync(User.GetUserId()!);
        if (user == null) return NotFound();

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Gender = request.Gender;
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();

        var result = await _users.UpdateAsync(user);
        if (!result.Succeeded)
            return Problem(detail: string.Join(" ", result.Errors.Select(e => e.Description)), statusCode: 400);

        return Ok(await ToDto(user));
    }

    /// <summary>Uploads a new profile picture to Azure Blob Storage.</summary>
    [HttpPost("picture")]
    public async Task<ActionResult<ProfileDto>> UploadPicture(IFormFile file, CancellationToken ct)
    {
        var error = ImageValidator.Validate(file);
        if (error != null) return Problem(detail: error, statusCode: 400);

        var user = await _users.FindByIdAsync(User.GetUserId()!);
        if (user == null) return NotFound();

        var oldBlob = user.ProfileImagePath;
        user.ProfileImagePath = await _files.UploadAsync(file, "profiles", ct);

        var result = await _users.UpdateAsync(user);
        if (!result.Succeeded)
            return Problem(detail: string.Join(" ", result.Errors.Select(e => e.Description)), statusCode: 400);

        await _files.DeleteAsync(oldBlob, ct);
        return Ok(await ToDto(user));
    }
}
