using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RunWay.Api.Data;
using RunWay.Api.Dtos;
using RunWay.Api.Services;

namespace RunWay.Api.Controllers;

[ApiController]
[Route("api")]
public class EnrolmentsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public EnrolmentsController(ApplicationDbContext db) => _db = db;

    private IQueryable<Enrolment> WithDetails() => _db.Enrolments
        .Include(e => e.Event)
        .Include(e => e.Category)
        .Include(e => e.Participant)
        .Include(e => e.Result);

    /// <summary>A participant enrols in an upcoming event and picks a category. Recorded as Pending.</summary>
    [HttpPost("enrolments")]
    [Authorize(Roles = Roles.Participant)]
    public async Task<ActionResult<EnrolmentDto>> Enrol(EnrolRequest request)
    {
        var userId = User.GetUserId()!;

        var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == request.EventId);
        if (ev == null) return Problem(detail: "Event not found.", statusCode: 404);
        if (ev.Date < RunWay.Api.Services.RaceClock.Now)
            return Problem(detail: "This event has already taken place, so enrolment is closed.", statusCode: 400);

        var categoryOk = await _db.Categories.AnyAsync(c => c.Id == request.CategoryId && c.EventId == request.EventId);
        if (!categoryOk)
            return Problem(detail: "The selected category does not belong to this event.", statusCode: 400);

        var existing = await _db.Enrolments.FirstOrDefaultAsync(e => e.EventId == request.EventId && e.ParticipantId == userId);
        if (existing != null && existing.Status != EnrolmentStatus.Cancelled)
            return Problem(detail: "You are already enrolled in this event.", statusCode: 409);

        if (existing != null)
        {
            // Re-enrolling after a cancellation re-uses the same record.
            existing.CategoryId = request.CategoryId;
            existing.Status = EnrolmentStatus.Pending;
            existing.EnrolledAt = DateTime.UtcNow;
        }
        else
        {
            existing = new Enrolment
            {
                EventId = request.EventId,
                CategoryId = request.CategoryId,
                ParticipantId = userId,
                Status = EnrolmentStatus.Pending
            };
            _db.Enrolments.Add(existing);
        }

        await _db.SaveChangesAsync();
        var saved = await WithDetails().AsNoTracking().FirstAsync(e => e.Id == existing.Id);
        return Ok(saved.ToDto());
    }

    /// <summary>All events the signed-in participant has enrolled in, with status.</summary>
    [HttpGet("enrolments/mine")]
    [Authorize(Roles = Roles.Participant)]
    public async Task<ActionResult<List<EnrolmentDto>>> Mine()
    {
        var userId = User.GetUserId();
        var list = await WithDetails().AsNoTracking()
            .Where(e => e.ParticipantId == userId)
            .OrderByDescending(e => e.Event!.Date)
            .ToListAsync();
        return Ok(list.Select(e => e.ToDto()).ToList());
    }

    [HttpPut("enrolments/{id:int}/cancel")]
    [Authorize(Roles = Roles.Participant)]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = User.GetUserId();
        var enrolment = await _db.Enrolments.Include(e => e.Event).Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.Id == id && e.ParticipantId == userId);
        if (enrolment == null) return NotFound();
        if (enrolment.Event!.Date < RunWay.Api.Services.RaceClock.Now)
            return Problem(detail: "You cannot cancel an enrolment for an event that has already taken place.", statusCode: 400);

        enrolment.Status = EnrolmentStatus.Cancelled;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Organiser view of everyone enrolled in one of their events.</summary>
    [HttpGet("events/{eventId:int}/enrolments")]
    [Authorize(Roles = Roles.Organiser + "," + Roles.Admin)]
    public async Task<ActionResult<List<EnrolmentDto>>> ForEvent(int eventId)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId);
        if (ev == null) return NotFound();
        if (!User.CanManage(ev)) return Forbid();

        var list = await WithDetails().AsNoTracking()
            .Where(e => e.EventId == eventId)
            .OrderBy(e => e.Participant!.LastName).ThenBy(e => e.Participant!.FirstName)
            .ToListAsync();
        return Ok(list.Select(e => e.ToDto()).ToList());
    }

    /// <summary>Organiser confirms (or rejects) a participant's enrolment.</summary>
    [HttpPut("enrolments/{id:int}/status")]
    [Authorize(Roles = Roles.Organiser + "," + Roles.Admin)]
    public async Task<IActionResult> SetStatus(int id, UpdateEnrolmentStatusRequest request)
    {
        var enrolment = await _db.Enrolments.Include(e => e.Event).Include(e => e.Result).FirstOrDefaultAsync(e => e.Id == id);
        if (enrolment == null) return NotFound();
        if (!User.CanManage(enrolment.Event!)) return Forbid();

        if (enrolment.Result != null && request.Status != EnrolmentStatus.Confirmed)
            return Problem(detail: "Remove the published result before changing this enrolment status.", statusCode: 409);
        enrolment.Status = request.Status;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
