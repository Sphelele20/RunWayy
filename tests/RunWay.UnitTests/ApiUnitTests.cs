using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RunWay.Api.Controllers;
using RunWay.Api.Data;
using RunWay.Api.Dtos;
using RunWay.Api.Services;
using Xunit;
namespace RunWay.UnitTests;
public sealed class ApiUnitTests : IDisposable {
 private readonly SqliteConnection connection=new("Data Source=:memory:");
 private readonly ServiceProvider services;
 private readonly ApplicationDbContext db;
 private readonly UserManager<ApplicationUser> users;
 private readonly FakeFiles files=new();
 public ApiUnitTests(){
  connection.Open();var registrations=new ServiceCollection();
  registrations.AddLogging();registrations.AddControllers();
  registrations.AddDbContext<ApplicationDbContext>(o=>o.UseSqlite(connection));
  registrations.AddIdentityCore<ApplicationUser>(o=>o.User.RequireUniqueEmail=true).AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
  services=registrations.BuildServiceProvider();db=services.GetRequiredService<ApplicationDbContext>();db.Database.EnsureCreated();users=services.GetRequiredService<UserManager<ApplicationUser>>();
 }
 private T As<T>(T controller,ApplicationUser? user=null,string role=Roles.Organiser)where T:ControllerBase{
  var identity=new ClaimsIdentity("unit", "name","role");
  if(user!=null){identity.AddClaim(new("sub",user.Id));identity.AddClaim(new("role",role));}
  controller.ControllerContext=new(){HttpContext=new DefaultHttpContext{RequestServices=services,User=new ClaimsPrincipal(identity)}};
  return controller;
 }
 private ITokenService Tokens()=>new TokenService(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Jwt:Key","UNIT-TEST-ONLY-32-BYTE-KEY-1234567890"},{"Jwt:Issuer","Unit"},{"Jwt:Audience","Unit"}}).Build());
 private async Task<ApplicationUser> User(string role=Roles.Organiser){
  var roles=services.GetRequiredService<RoleManager<IdentityRole>>();
  if(!await roles.RoleExistsAsync(role))Assert.True((await roles.CreateAsync(new(role))).Succeeded);
  var email=Guid.NewGuid()+"@unit.local";var u=new ApplicationUser{UserName=email,Email=email,FirstName="Unit",LastName="Runner",EmailConfirmed=true};
  Assert.True((await users.CreateAsync(u,"UnitPassword!42")).Succeeded);Assert.True((await users.AddToRoleAsync(u,role)).Succeeded);return u;
 }
 private async Task<(ApplicationUser Owner,Event Race,Category Category)> Race(bool past=false){
  var owner=await User();var race=new Event{OrganiserId=owner.Id,Name="Unit Race",Description="Unit fixture",Date=past?RaceClock.Now.AddDays(-1):RaceClock.Now.AddDays(1),Location="Test",DistanceKm=10,EventType=EventType.Run};
  db.Events.Add(race);await db.SaveChangesAsync();var c=new Category{EventId=race.Id,Name="Open",DistanceKm=10};db.Categories.Add(c);await db.SaveChangesAsync();return(owner,race,c);
 }
 private static EventRequest Request(DateTime? date=null)=>new(){Name="New race",Description="A test race",Location="Test",DistanceKm=10,Date=date??RaceClock.Now.AddDays(1),EventType=EventType.Run};
 [Theory][InlineData(Roles.Organiser)][InlineData(Roles.Participant)]
 public async Task Registration_assigns_only_the_requested_coursework_role(string role){
  var roles=services.GetRequiredService<RoleManager<IdentityRole>>();await roles.CreateAsync(new(role));
  var request=new RegisterRequest{FirstName="New",LastName="User",Email="new@unit.local",Password="UnitPassword!42",Role=role};
  var result=await As(new AuthController(users,Tokens())).Register(request);
  var auth=Assert.IsType<AuthResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
  Assert.Equal(new[]{role},auth.Roles);Assert.NotEmpty(auth.Token);
 }
 [Fact]public async Task Registration_cannot_self_assign_admin(){
  var response=await As(new AuthController(users,Tokens())).Register(new(){FirstName="Bad",LastName="Admin",Email="bad@unit.local",Password="UnitPassword!42",Role=Roles.Admin});
  Assert.Equal(400,Assert.IsType<ObjectResult>(response.Result).StatusCode);Assert.Empty(await db.Users.ToListAsync());
 }
 [Fact]public async Task Duplicate_registration_does_not_create_a_second_account(){
  var u=await User(Roles.Participant);var response=await As(new AuthController(users,Tokens())).Register(new(){Email=u.Email!,Password="UnitPassword!42",FirstName="Duplicate",LastName="User"});
  Assert.Equal(400,Assert.IsType<ObjectResult>(response.Result).StatusCode);Assert.Equal(1,await db.Users.CountAsync());
 }
 [Fact]public async Task Identity_stores_a_verifiable_hash_instead_of_the_password(){
  var u=await User();Assert.NotEqual("UnitPassword!42",u.PasswordHash);
  Assert.NotEqual(PasswordVerificationResult.Failed,users.PasswordHasher.VerifyHashedPassword(u,u.PasswordHash!,"UnitPassword!42"));
 }
 [Fact]public async Task Valid_login_returns_identity_and_role(){
  var u=await User(Roles.Participant);var response=await As(new AuthController(users,Tokens())).Login(new(){Email=u.Email!,Password="UnitPassword!42"});
  var auth=Assert.IsType<AuthResponse>(Assert.IsType<OkObjectResult>(response.Result).Value);Assert.Equal(u.Id,auth.UserId);Assert.Contains(Roles.Participant,auth.Roles);
 }
 [Fact]public async Task Wrong_password_is_rejected(){
  var u=await User();var response=await As(new AuthController(users,Tokens())).Login(new(){Email=u.Email!,Password="Wrong!42"});
  Assert.Equal(401,Assert.IsType<ObjectResult>(response.Result).StatusCode);
 }
 [Fact]public async Task Event_creation_persists_the_authenticated_owner(){
  var u=await User();var response=await As(new EventsController(db,files),u).Create(Request());
  var dto=Assert.IsType<EventDto>(Assert.IsType<CreatedAtActionResult>(response.Result).Value);
  Assert.Equal(u.Id,dto.OrganiserId);Assert.Equal(1,await db.Events.CountAsync());
 }
 [Fact]public async Task New_event_in_the_past_is_rejected(){
  var u=await User();var response=await As(new EventsController(db,files),u).Create(Request(RaceClock.Now.AddDays(-1)));
  Assert.Equal(400,Assert.IsType<ObjectResult>(response.Result).StatusCode);Assert.Empty(await db.Events.ToListAsync());
 }
 [Fact]public async Task Another_organiser_cannot_edit_or_delete_an_event(){
  var f=await Race();var other=await User();var controller=As(new EventsController(db,files),other);
  Assert.IsType<ForbidResult>((await controller.Update(f.Race.Id,Request())).Result);
  Assert.IsType<ForbidResult>(await controller.Delete(f.Race.Id,default));Assert.Equal("Unit Race",(await db.Events.SingleAsync()).Name);
 }
 [Fact]public async Task Enrolment_records_the_participant_event_category_and_pending_status(){
  var f=await Race();var runner=await User(Roles.Participant);
  var response=await As(new EnrolmentsController(db),runner,Roles.Participant).Enrol(new(){EventId=f.Race.Id,CategoryId=f.Category.Id});
  var dto=Assert.IsType<EnrolmentDto>(Assert.IsType<OkObjectResult>(response.Result).Value);
  Assert.Equal(runner.Id,dto.ParticipantId);Assert.Equal(f.Race.Id,dto.EventId);Assert.Equal(f.Category.Id,dto.CategoryId);Assert.Equal(EnrolmentStatus.Pending,dto.Status);
 }
 [Fact]public async Task Duplicate_enrolment_is_rejected_without_a_second_record(){
  var f=await Race();var runner=await User(Roles.Participant);var controller=As(new EnrolmentsController(db),runner,Roles.Participant);
  var input=new EnrolRequest{EventId=f.Race.Id,CategoryId=f.Category.Id};await controller.Enrol(input);
  Assert.Equal(409,Assert.IsType<ObjectResult>((await controller.Enrol(input)).Result).StatusCode);Assert.Equal(1,await db.Enrolments.CountAsync());
 }
 [Fact]public async Task A_category_from_another_event_cannot_be_selected(){
  var f=await Race();var second=await Race();var runner=await User(Roles.Participant);
  var response=await As(new EnrolmentsController(db),runner,Roles.Participant).Enrol(new(){EventId=f.Race.Id,CategoryId=second.Category.Id});
  Assert.Equal(400,Assert.IsType<ObjectResult>(response.Result).StatusCode);Assert.Empty(await db.Enrolments.ToListAsync());
 }
 [Fact]public async Task Past_event_does_not_accept_enrolment(){
  var f=await Race(true);var runner=await User(Roles.Participant);
  var response=await As(new EnrolmentsController(db),runner,Roles.Participant).Enrol(new(){EventId=f.Race.Id,CategoryId=f.Category.Id});
  Assert.Equal(400,Assert.IsType<ObjectResult>(response.Result).StatusCode);
 }
 [Fact]public async Task Another_organiser_cannot_change_an_enrolment_status(){
  var f=await Race();var runner=await User(Roles.Participant);var en=new Enrolment{EventId=f.Race.Id,CategoryId=f.Category.Id,ParticipantId=runner.Id};db.Enrolments.Add(en);await db.SaveChangesAsync();
  Assert.IsType<ForbidResult>(await As(new EnrolmentsController(db),await User()).SetStatus(en.Id,new(){Status=EnrolmentStatus.Confirmed}));
  Assert.Equal(EnrolmentStatus.Pending,en.Status);
 }
 [Fact]public async Task Results_are_rejected_before_an_event(){
  var f=await Race();var runner=await User(Roles.Participant);var en=new Enrolment{EventId=f.Race.Id,CategoryId=f.Category.Id,ParticipantId=runner.Id,Status=EnrolmentStatus.Confirmed};db.Enrolments.Add(en);await db.SaveChangesAsync();
  var result=await As(new ResultsController(db),f.Owner).Save(new(){EnrolmentId=en.Id,FinishTime=TimeSpan.FromMinutes(50),Position=1});
  Assert.Equal(400,Assert.IsType<ObjectResult>(result).StatusCode);Assert.Empty(await db.Results.ToListAsync());
 }
 [Fact]public async Task Published_results_keep_their_enrolment_confirmed(){
  var f=await Race(true);var runner=await User(Roles.Participant);var en=new Enrolment{EventId=f.Race.Id,CategoryId=f.Category.Id,ParticipantId=runner.Id,Status=EnrolmentStatus.Confirmed};db.Enrolments.Add(en);await db.SaveChangesAsync();
  Assert.IsType<NoContentResult>(await As(new ResultsController(db),f.Owner).Save(new(){EnrolmentId=en.Id,FinishTime=TimeSpan.FromMinutes(50),Position=1}));
  var changed=await As(new EnrolmentsController(db),f.Owner).SetStatus(en.Id,new(){Status=EnrolmentStatus.Cancelled});
  Assert.Equal(409,Assert.IsType<ObjectResult>(changed).StatusCode);Assert.Equal(EnrolmentStatus.Confirmed,en.Status);
 }
 public void Dispose(){services.Dispose();connection.Dispose();}
 private sealed class FakeFiles:IFileStorageService{
  public Task<string> UploadAsync(IFormFile f,string folder,CancellationToken ct=default)=>Task.FromResult(folder+"/unit.png");
  public Task<(Stream Content,string ContentType)?> OpenReadAsync(string name,CancellationToken ct=default)=>Task.FromResult<(Stream,string)?>(null);
  public Task DeleteAsync(string? name,CancellationToken ct=default)=>Task.CompletedTask;
 }
}
