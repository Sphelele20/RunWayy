using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RunWay.Api.Data;
using RunWay.Api.Dtos;
using RunWay.Api.Services;

namespace RunWay.Api.Controllers;

[ApiController]
[Route("api")]
public class ResultsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ResultsController(ApplicationDbContext db) => _db = db;

    private static ResultDto ToDto(RaceResult r, int totalFinishers) => new()
    {
        EnrolmentId = r.EnrolmentId,
        EventId = r.Enrolment!.EventId,
        EventName = r.Enrolment.Event!.Name,
        EventDate = r.Enrolment.Event.Date,
        CategoryName = r.Enrolment.Category!.Name,
        ParticipantName = r.Enrolment.Participant!.FullName,
        FinishTime = r.FinishTime,
        Position = r.Position,
        TotalFinishers = totalFinishers
    };

    private IQueryable<RaceResult> WithDetails() => _db.Results
        .Include(r => r.Enrolment).ThenInclude(e => e!.Event)
        .Include(r => r.Enrolment).ThenInclude(e => e!.Category)
        .Include(r => r.Enrolment).ThenInclude(e => e!.Participant);

    /// <summary>Public results table for one event, ordered by position.</summary>
    [HttpGet("events/{eventId:int}/results")]
    public async Task<ActionResult<List<ResultDto>>> ForEvent(int eventId)
    {
        var results = await WithDetails().AsNoTracking()
            .Where(r => r.Enrolment!.EventId == eventId)
            .OrderBy(r => r.Position)
            .ToListAsync();
        return Ok(results.Select(r => ToDto(r, results.Count)).ToList());
    }

    /// <summary>The signed-in participant's race history, e.g. "47th of 312 finishers".</summary>
    [HttpGet("results/mine")]
    [Authorize(Roles = Roles.Participant)]
    public async Task<ActionResult<List<ResultDto>>> Mine()
    {
        var userId = User.GetUserId();
        var mine = await WithDetails().AsNoTracking()
            .Where(r => r.Enrolment!.ParticipantId == userId)
            .OrderByDescending(r => r.Enrolment!.Event!.Date)
            .ToListAsync();

        var eventIds = mine.Select(r => r.Enrolment!.EventId).Distinct().ToList();
        var totals = await _db.Results.AsNoTracking()
            .Where(r => eventIds.Contains(r.Enrolment!.EventId))
            .GroupBy(r => r.Enrolment!.EventId)
            .Select(g => new { EventId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EventId, x => x.Count);

        return Ok(mine.Select(r => ToDto(r, totals.GetValueOrDefault(r.Enrolment!.EventId))).ToList());
    }

    /// <summary>Organiser captures (or corrects) a finish time and position once the event has concluded.</summary>
    [HttpPut("results")]
    [Authorize(Roles = Roles.Organiser + "," + Roles.Admin)]
    public async Task<IActionResult> Save(ResultRequest request)
    {
        var enrolment = await _db.Enrolments.Include(e => e.Event).Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.Id == request.EnrolmentId);
        if (enrolment == null) return NotFound();
        if (!User.CanManage(enrolment.Event!)) return Forbid();

        if (enrolment.Event!.Date > RunWay.Api.Services.RaceClock.Now)
            return Problem(detail: "Results can only be entered after the event has taken place.", statusCode: 400);
        if (enrolment.Status != EnrolmentStatus.Confirmed)
            return Problem(detail: "Only confirmed participants can have a result.", statusCode: 400);
        if (request.FinishTime <= TimeSpan.Zero)
            return Problem(detail: "Finish time must be greater than zero.", statusCode: 400);

        var positionTaken = await _db.Results.AnyAsync(r =>
            r.Enrolment!.EventId == enrolment.EventId && r.Position == request.Position && r.EnrolmentId != enrolment.Id);
        if (positionTaken)
            return Problem(detail: $"Position {request.Position} has already been given to another participant.", statusCode: 409);

        if (enrolment.Result == null)
        {
            _db.Results.Add(new RaceResult
            {
                EventId = enrolment.EventId,
                EnrolmentId = enrolment.Id,
                FinishTime = request.FinishTime,
                Position = request.Position
            });
        }
        else
        {
            enrolment.Result.FinishTime = request.FinishTime;
            enrolment.Result.Position = request.Position;
            enrolment.Result.PublishedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("results/{enrolmentId:int}")]
    [Authorize(Roles = Roles.Organiser + "," + Roles.Admin)]
    public async Task<IActionResult> Delete(int enrolmentId)
    {
        var result = await _db.Results.Include(r => r.Enrolment).ThenInclude(e => e!.Event)
            .FirstOrDefaultAsync(r => r.EnrolmentId == enrolmentId);
        if (result == null) return NotFound();
        if (!User.CanManage(result.Enrolment!.Event!)) return Forbid();

        _db.Results.Remove(result);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
