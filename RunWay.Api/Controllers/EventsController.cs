using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RunWay.Api.Data;
using RunWay.Api.Dtos;
using RunWay.Api.Services;

namespace RunWay.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _files;

    public EventsController(ApplicationDbContext db, IFileStorageService files)
    {
        _db = db;
        _files = files;
    }

    private IQueryable<Event> EventsWithDetails() => _db.Events
        .Include(e => e.Organiser)
        .Include(e => e.Enrolments)
        .Include(e => e.Categories).ThenInclude(c => c.Enrolments);

    /// <summary>Public list of events. By default only upcoming events are returned.</summary>
    [HttpGet]
    public async Task<ActionResult<List<EventDto>>> GetAll([FromQuery] EventFilter filter)
    {
        var query = EventsWithDetails().AsNoTracking().AsSplitQuery();

        if (!filter.IncludePast) query = query.Where(e => e.Date >= RunWay.Api.Services.RaceClock.Now);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            query = query.Where(e => e.Name.Contains(s) || e.Location.Contains(s) || e.Description.Contains(s));
        }
        if (filter.EventType == EventType.Run) query = query.Where(e => e.EventType != EventType.Walk && e.EventType != EventType.Cycle);
        else if (filter.EventType.HasValue) query = query.Where(e => e.EventType == filter.EventType.Value);
        if (filter.From.HasValue) query = query.Where(e => e.Date >= filter.From.Value.Date);
        if (filter.To.HasValue) query = query.Where(e => e.Date < filter.To.Value.Date.AddDays(1));
        if (filter.MinDistance.HasValue)
            query = query.Where(e => e.DistanceKm >= filter.MinDistance.Value || e.Categories.Any(c => c.DistanceKm >= filter.MinDistance.Value));
        if (filter.MaxDistance.HasValue)
            query = query.Where(e => e.DistanceKm <= filter.MaxDistance.Value || e.Categories.Any(c => c.DistanceKm <= filter.MaxDistance.Value));

        var events = await query.OrderBy(e => e.Date).ToListAsync();
        return Ok(events.Select(e => e.ToDto(includeCategories: true)).ToList());
    }

    /// <summary>All events created by the signed-in organiser (past and upcoming). Admins see every event.</summary>
    [HttpGet("mine")]
    [Authorize(Roles = Roles.Organiser + "," + Roles.Admin)]
    public async Task<ActionResult<List<EventDto>>> Mine()
    {
        var userId = User.GetUserId();
        var query = EventsWithDetails().AsNoTracking().AsSplitQuery();
        if (!User.IsInRole(Roles.Admin)) query = query.Where(e => e.OrganiserId == userId);

        var events = await query.OrderByDescending(e => e.Date).ToListAsync();
        return Ok(events.Select(e => e.ToDto(includeCategories: true)).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EventDto>> Get(int id)
    {
        var ev = await EventsWithDetails().AsNoTracking().AsSplitQuery().FirstOrDefaultAsync(e => e.Id == id);
        return ev == null ? NotFound() : Ok(ev.ToDto(includeCategories: true));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Organiser + "," + Roles.Admin)]
    public async Task<ActionResult<EventDto>> Create(EventRequest request)
    {
        if (request.Date <= RaceClock.Now) return Problem(detail: "A new event must be in the future (South African time).", statusCode: 400);
        var ev = new Event
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Date = request.Date,
            Location = request.Location.Trim(),
            DistanceKm = request.DistanceKm,
            EventType = request.EventType,
            OrganiserId = User.GetUserId()!
        };
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        var created = await EventsWithDetails().AsNoTracking().AsSplitQuery().FirstAsync(e => e.Id == ev.Id);
        return CreatedAtAction(nameof(Get), new { id = ev.Id }, created.ToDto(includeCategories: true));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Organiser + "," + Roles.Admin)]
    public async Task<ActionResult<EventDto>> Update(int id, EventRequest request)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == id);
        if (ev == null) return NotFound();
        if (!User.CanManage(ev)) return Forbid();

        if (ev.Date != request.Date && await _db.Results.AnyAsync(r => r.EventId == id))
            return Problem(detail: "An event with published results cannot be rescheduled.", statusCode: 409);
        ev.Name = request.Name.Trim();
        ev.Description = request.Description.Trim();
        ev.Date = request.Date;
        ev.Location = request.Location.Trim();
        ev.DistanceKm = request.DistanceKm;
        ev.EventType = request.EventType;
        await _db.SaveChangesAsync();

        var updated = await EventsWithDetails().AsNoTracking().AsSplitQuery().FirstAsync(e => e.Id == id);
        return Ok(updated.ToDto(includeCategories: true));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Organiser + "," + Roles.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (ev == null) return NotFound();
        if (!User.CanManage(ev)) return Forbid();


        var enrolments = await _db.Enrolments.Include(x => x.Result).Where(x => x.EventId == id).ToListAsync(ct);
        _db.Enrolments.RemoveRange(enrolments);
        var banner = ev.BannerBlobName;
        _db.Events.Remove(ev); // categories, enrolments and results are removed by cascade
        await _db.SaveChangesAsync(ct);

        await _files.DeleteAsync(banner, ct);
        return NoContent();
    }

    /// <summary>Uploads the event banner image to Azure Blob Storage.</summary>
    [HttpPost("{id:int}/banner")]
    [Authorize(Roles = Roles.Organiser + "," + Roles.Admin)]
    public async Task<ActionResult<EventDto>> UploadBanner(int id, IFormFile file, CancellationToken ct)
    {
        var error = ImageValidator.Validate(file);
        if (error != null) return Problem(detail: error, statusCode: 400);

        var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (ev == null) return NotFound();
        if (!User.CanManage(ev)) return Forbid();

        var old = ev.BannerBlobName;
        ev.BannerBlobName = await _files.UploadAsync(file, "events", ct);
        await _db.SaveChangesAsync(ct);
        await _files.DeleteAsync(old, ct);

        var updated = await EventsWithDetails().AsNoTracking().AsSplitQuery().FirstAsync(e => e.Id == id, ct);
        return Ok(updated.ToDto(includeCategories: true));
    }
}
