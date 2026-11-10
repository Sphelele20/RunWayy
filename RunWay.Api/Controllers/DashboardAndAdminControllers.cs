using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RunWay.Api.Data;
using RunWay.Api.Dtos;
using RunWay.Api.Services;

namespace RunWay.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public DashboardController(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    /// <summary>Summary for the Organiser: events created, total enrolments and upcoming event dates.</summary>
    [HttpGet("organiser")]
    [Authorize(Roles = Roles.Organiser)]
    public async Task<ActionResult<OrganiserDashboardDto>> Organiser()
    {
        var userId = User.GetUserId();
        var now = RunWay.Api.Services.RaceClock.Now;

        var mine = _db.Events.AsNoTracking().Where(e => e.OrganiserId == userId);

        var upcoming = await mine.Where(e => e.Date >= now)
            .OrderBy(e => e.Date)
            .Select(e => new UpcomingEventDto
            {
                Id = e.Id,
                Name = e.Name,
                Date = e.Date,
                Location = e.Location,
                EnrolmentCount = e.Enrolments.Count(x => x.Status != EnrolmentStatus.Cancelled)
            })
            .ToListAsync();

        var activeEnrolments = _db.Enrolments.AsNoTracking()
            .Where(e => e.Event!.OrganiserId == userId && e.Status != EnrolmentStatus.Cancelled);

        return Ok(new OrganiserDashboardDto
        {
            TotalEvents = await mine.CountAsync(),
            TotalEnrolments = await activeEnrolments.CountAsync(),
            PendingEnrolments = await activeEnrolments.CountAsync(e => e.Status == EnrolmentStatus.Pending),
            UpcomingEvents = upcoming
        });
    }

    [HttpGet("admin")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<AdminDashboardDto>> Admin()
    {
        var dto = new AdminDashboardDto
        {
            TotalUsers = await _users.Users.CountAsync(),
            TotalEvents = await _db.Events.CountAsync(),
            TotalEnrolments = await _db.Enrolments.CountAsync(e => e.Status != EnrolmentStatus.Cancelled)
        };
        foreach (var role in Roles.All)
            dto.UsersByRole[role] = (await _users.GetUsersInRoleAsync(role)).Count;

        return Ok(dto);
    }
}

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;

    public AdminController(UserManager<ApplicationUser> users) => _users = users;

    [HttpGet("users")]
    public async Task<ActionResult<List<UserWithRolesDto>>> Users()
    {
        var all = await _users.Users.AsNoTracking().OrderBy(u => u.Email).ToListAsync();
        var result = new List<UserWithRolesDto>();
        foreach (var u in all)
        {
            result.Add(new UserWithRolesDto
            {
                Id = u.Id,
                Email = u.Email ?? string.Empty,
                FullName = u.FullName,
                Roles = (await _users.GetRolesAsync(u)).ToList()
            });
        }
        return Ok(result);
    }

    [HttpGet("users/{id}")]
    public async Task<ActionResult<UserWithRolesDto>> GetUser(string id)
    {
        var u = await _users.FindByIdAsync(id);
        if (u == null) return NotFound();
        return Ok(new UserWithRolesDto
        {
            Id = u.Id,
            Email = u.Email ?? string.Empty,
            FullName = u.FullName,
            Roles = (await _users.GetRolesAsync(u)).ToList()
        });
    }

    [HttpPut("users/{id}/roles")]
    public async Task<IActionResult> SetRoles(string id, UpdateRolesRequest request)
    {
        var user = await _users.FindByIdAsync(id);
        if (user == null) return NotFound();

        var selected = request.Roles.Where(r => Roles.All.Contains(r)).Distinct().ToList();

        // Don't let an admin remove their own Admin role.
        if (user.Id == User.GetUserId() && !selected.Contains(Roles.Admin))
            selected.Add(Roles.Admin);

        var current = await _users.GetRolesAsync(user);

        var toRemove = current.Except(selected).ToList();
        if (toRemove.Count > 0)
        {
            var removed = await _users.RemoveFromRolesAsync(user, toRemove);
            if (!removed.Succeeded) return Problem(detail: "Cannot remove the user's existing roles.", statusCode: 400);
        }

        var toAdd = selected.Except(current).ToList();
        if (toAdd.Count > 0)
        {
            var added = await _users.AddToRolesAsync(user, toAdd);
            if (!added.Succeeded) return Problem(detail: "Cannot add the selected roles.", statusCode: 400);
        }

        await _users.UpdateSecurityStampAsync(user);
        return NoContent();
    }
}
