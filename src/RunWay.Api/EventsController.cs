using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RunWay.Contracts;
namespace RunWay.Api;
[ApiController,Route("api/events")]
public class EventsController(RaceDb db,ImageStore images) : ControllerBase {
 private string UserId=>User.FindFirstValue(ClaimTypes.NameIdentifier)!;
 private IQueryable<RaceEvent> Full=>db.Events.Include(e=>e.Categories).Include(e=>e.Enrolments);
 [HttpGet,AllowAnonymous]
 public async Task<IEnumerable<EventDto>> List(string? search,string? type,bool mine=false,bool upcoming=true){
  var q=Full.AsNoTracking();
  if(mine){if(!User.IsInRole("Organiser")){Response.StatusCode=403;return [];}q=q.Where(e=>e.OrganiserId==UserId);}
  if(upcoming)q=q.Where(e=>e.Date>DateTime.UtcNow);
  if(!string.IsNullOrWhiteSpace(search))q=q.Where(e=>e.Name.Contains(search)||e.Location.Contains(search));
  if(!string.IsNullOrEmpty(type))q=q.Where(e=>e.EventType==type);
  return (await q.OrderBy(e=>e.Date).ToListAsync()).Select(e=>e.Dto());
 }
 [HttpGet("{id:int}"),AllowAnonymous]
 public async Task<IActionResult> Get(int id){var e=await Full.AsNoTracking().SingleOrDefaultAsync(e=>e.Id==id);return e is null?NotFound():Ok(e.Dto());}
 [HttpPost,Authorize(Roles="Organiser")]
 public async Task<IActionResult> Create(EventRequest input){
  if(input.Date<=DateTime.UtcNow)return BadRequest(new{message="Choose a future event date."});
  var e=new RaceEvent{OrganiserId=UserId};Assign(e,input);db.Events.Add(e);await db.SaveChangesAsync();return CreatedAtAction(nameof(Get),new{id=e.Id},e.Dto());
 }
 private static void Assign(RaceEvent e,EventRequest x){e.Name=x.Name.Trim();e.Description=x.Description.Trim();e.Location=x.Location.Trim();e.Date=DateTime.SpecifyKind(x.Date,DateTimeKind.Utc);e.Distance=x.Distance;e.EventType=x.EventType;}
 [HttpPut("{id:int}"),Authorize(Roles="Organiser")]
 public async Task<IActionResult> Update(int id,EventRequest input){
  var e=await Full.SingleOrDefaultAsync(e=>e.Id==id);if(e is null)return NotFound();if(e.OrganiserId!=UserId)return Forbid();
  if(e.Date<=DateTime.UtcNow || input.Date<=DateTime.UtcNow)return BadRequest(new{message="Only upcoming events can be edited. Choose a future date."});
  Assign(e,input);await db.SaveChangesAsync();return Ok(e.Dto());
 }
 [HttpDelete("{id:int}"),Authorize(Roles="Organiser")]
 public async Task<IActionResult> Delete(int id){
  var e=await Full.SingleOrDefaultAsync(e=>e.Id==id);if(e is null)return NotFound();if(e.OrganiserId!=UserId)return Forbid();
  await using var transaction=await db.Database.BeginTransactionAsync();db.Enrolments.RemoveRange(e.Enrolments);await db.SaveChangesAsync();db.Events.Remove(e);await db.SaveChangesAsync();await transaction.CommitAsync();return NoContent();
 }
 [HttpPost("{id:int}/banner"),Authorize(Roles="Organiser"),RequestSizeLimit(3*1024*1024)]
 public async Task<IActionResult> Banner(int id,IFormFile file){
  var e=await db.Events.FindAsync(id);if(e is null)return NotFound();if(e.OrganiserId!=UserId)return Forbid();
  e.BannerUrl=await images.Save(file);await db.SaveChangesAsync();return Ok(new{e.BannerUrl});
 }
 [HttpPost("{id:int}/categories"),Authorize(Roles="Organiser")]
 public async Task<IActionResult> Category(int id,CategoryRequest input){
  var e=await db.Events.FindAsync(id);if(e is null)return NotFound();if(e.OrganiserId!=UserId)return Forbid();
  var c=new Category{EventId=id,Name=input.Name.Trim(),Description=input.Description};db.Categories.Add(c);await db.SaveChangesAsync();return Ok(new CategoryDto{Id=c.Id,Name=c.Name,Description=c.Description});
 }
 [HttpDelete("{id:int}/categories/{categoryId:int}"),Authorize(Roles="Organiser")]
 public async Task<IActionResult> RemoveCategory(int id,int categoryId){
  var e=await db.Events.FindAsync(id);if(e is null)return NotFound();if(e.OrganiserId!=UserId)return Forbid();
  var c=await db.Categories.SingleOrDefaultAsync(c=>c.Id==categoryId && c.EventId==id);if(c is null)return NotFound();
  if(await db.Enrolments.AnyAsync(n=>n.CategoryId==categoryId))return Conflict(new{message="This category has enrolments and cannot be removed."});
  db.Categories.Remove(c);await db.SaveChangesAsync();return NoContent();
 }
 [HttpPost("{id:int}/enrolments"),Authorize(Roles="Participant")]
 public async Task<IActionResult> Enrol(int id,EnrolRequest input){
  var e=await db.Events.FindAsync(id);if(e is null)return NotFound();if(e.Date<=DateTime.UtcNow)return BadRequest(new{message="Entries are closed."});
  if(!await db.Categories.AnyAsync(c=>c.Id==input.CategoryId && c.EventId==id))return BadRequest(new{message="Select a category belonging to this event."});
  if(await db.Enrolments.AnyAsync(n=>n.EventId==id && n.ParticipantId==UserId))return Conflict(new{message="You have already entered this event."});
  var n=new Enrolment{EventId=id,CategoryId=input.CategoryId,ParticipantId=UserId};db.Enrolments.Add(n);await db.SaveChangesAsync();return StatusCode(201,new{n.Id,n.Status});
 }
 [HttpGet("{id:int}/enrolments"),Authorize(Roles="Organiser")]
 public async Task<IActionResult> Enrolments(int id){
  var e=await db.Events.FindAsync(id);if(e is null)return NotFound();if(e.OrganiserId!=UserId)return Forbid();
  return Ok((await db.Enrolments.Include(n=>n.Event).Include(n=>n.Category).Include(n=>n.Participant).Where(n=>n.EventId==id).ToListAsync()).Select(n=>n.Dto()));
 }
 [HttpPut("{id:int}/enrolments/{enrolmentId:int}/result"),Authorize(Roles="Organiser")]
 public async Task<IActionResult> Result(int id,int enrolmentId,ResultRequest input){
  var e=await db.Events.FindAsync(id);if(e is null)return NotFound();if(e.OrganiserId!=UserId)return Forbid();
  if(e.Date>DateTime.UtcNow)return BadRequest(new{message="Results can be published after the event."});
  var n=await db.Enrolments.SingleOrDefaultAsync(n=>n.Id==enrolmentId && n.EventId==id);if(n is null)return NotFound();
  if(input.FinishTime.Replace(":", "").All(c=>c=='0'))return BadRequest(new{message="Finish time must be greater than zero."});
  n.FinishTime=input.FinishTime;n.Position=input.Position;n.Status="Completed";await db.SaveChangesAsync();return NoContent();
 }
}
[ApiController,Route("api/me"),Authorize(Roles="Participant")]
public class ParticipantController(RaceDb db) : ControllerBase {
 [HttpGet("enrolments")]
 public async Task<IEnumerable<EnrolmentDto>> Get(){
  var id=User.FindFirstValue(ClaimTypes.NameIdentifier);
  return (await db.Enrolments.Include(n=>n.Event).Include(n=>n.Category).Include(n=>n.Participant).Where(n=>n.ParticipantId==id).OrderByDescending(n=>n.Event.Date).ToListAsync()).Select(n=>n.Dto());
 }
}
