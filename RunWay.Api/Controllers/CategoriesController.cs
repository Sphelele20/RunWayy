using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RunWay.Api.Data;
using RunWay.Api.Dtos;
using RunWay.Api.Services;

namespace RunWay.Api.Controllers;

[ApiController]
[Route("api")]
public class CategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CategoriesController(ApplicationDbContext db) => _db = db;

    [HttpGet("events/{eventId:int}/categories")]
    public async Task<ActionResult<List<CategoryDto>>> ForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.Id == eventId)) return NotFound();

        var categories = await _db.Categories.AsNoTracking()
            .Include(c => c.Enrolments)
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.DistanceKm)
            .ToListAsync();
        return Ok(categories.Select(c => c.ToDto()).ToList());
    }

    [HttpPost("events/{eventId:int}/categories")]
    [Authorize(Roles = Roles.Organiser + "," + Roles.Admin)]
    public async Task<ActionResult<CategoryDto>> Create(int eventId, CategoryRequest request)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == eventId);
        if (ev == null) return NotFound();
        if (!User.CanManage(ev)) return Forbid();

        var name = request.Name.Trim();
        if (await _db.Categories.AnyAsync(c => c.EventId == eventId && c.Name == name))
            return Problem(detail: "This event already has a category with that name.", statusCode: 409);

        var category = new Category { EventId = eventId, Name = name, DistanceKm = request.DistanceKm };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return Ok(category.ToDto());
    }

    [HttpPut("categories/{id:int}")]
    [Authorize(Roles = Roles.Organiser + "," + Roles.Admin)]
    public async Task<ActionResult<CategoryDto>> Update(int id, CategoryRequest request)
    {
        var category = await _db.Categories.Include(c => c.Event).Include(c => c.Enrolments)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (category == null) return NotFound();
        if (!User.CanManage(category.Event!)) return Forbid();

        var name = request.Name.Trim();
        if (await _db.Categories.AnyAsync(c => c.EventId == category.EventId && c.Name == name && c.Id != id))
            return Problem(detail: "This event already has a category with that name.", statusCode: 409);

        category.Name = name;
        category.DistanceKm = request.DistanceKm;
        await _db.SaveChangesAsync();
        return Ok(category.ToDto());
    }

    [HttpDelete("categories/{id:int}")]
    [Authorize(Roles = Roles.Organiser + "," + Roles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.Id == id);
        if (category == null) return NotFound();
        if (!User.CanManage(category.Event!)) return Forbid();

        if (await _db.Enrolments.AnyAsync(e => e.CategoryId == id))
            return Problem(detail: "This category has enrolments and cannot be deleted.", statusCode: 409);

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
