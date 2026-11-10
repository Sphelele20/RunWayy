using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RunWay.Contracts;
namespace RunWay.Api;
public class ApplicationUser : IdentityUser {
 public string FirstName {get;set;}="";
 public string LastName {get;set;}="";
 public string Gender {get;set;}="PreferNotToSay";
 public string Role {get;set;}="Participant";
 public string? ImageUrl {get;set;}
 public ProfileDto Dto()=>new(){Id=Id,Email=Email!,FirstName=FirstName,LastName=LastName,Gender=Gender,Role=Role,PhoneNumber=PhoneNumber,ImageUrl=ImageUrl};
}
public class RaceEvent {
 public int Id {get;set;}
 public string OrganiserId {get;set;}="";
 public ApplicationUser Organiser {get;set;}=null!;
 public string Name {get;set;}="";
 public string Description {get;set;}="";
 public string Location {get;set;}="";
 public DateTime Date {get;set;}
 public double Distance {get;set;}
 public string EventType {get;set;}="";
 public string? BannerUrl {get;set;}
 public List<Category> Categories {get;set;}=[];
 public List<Enrolment> Enrolments {get;set;}=[];
 public EventDto Dto()=>new(){Id=Id,OrganiserId=OrganiserId,Name=Name,Description=Description,Location=Location,Date=Date,Distance=Distance,EventType=EventType,BannerUrl=BannerUrl,Categories=Categories.Select(c=>new CategoryDto{Id=c.Id,Name=c.Name,Description=c.Description}).ToList(),EnrolmentCount=Enrolments.Count};
}
public class Category {
 public int Id {get;set;}
 public int EventId {get;set;}
 public RaceEvent Event {get;set;}=null!;
 public string Name {get;set;}="";
 public string? Description {get;set;}
}
public class Enrolment {
 public int Id {get;set;}
 public string ParticipantId {get;set;}="";
 public ApplicationUser Participant {get;set;}=null!;
 public int EventId {get;set;}
 public RaceEvent Event {get;set;}=null!;
 public int CategoryId {get;set;}
 public Category Category {get;set;}=null!;
 public string Status {get;set;}="Confirmed";
 public string? FinishTime {get;set;}
 public int? Position {get;set;}
 public EnrolmentDto Dto()=>new(){Id=Id,EventId=EventId,EventName=Event.Name,Date=Event.Date,EventType=Event.EventType,ParticipantName=Participant.FirstName+" "+Participant.LastName,Category=Category.Name,Status=Status,FinishTime=FinishTime,Position=Position};
}
public class ApiToken {
 public string Hash {get;set;}="";
 public string UserId {get;set;}="";
 public ApplicationUser User {get;set;}=null!;
 public DateTime ExpiresAt {get;set;}
}
public class RaceDb(DbContextOptions<RaceDb> options) : IdentityDbContext<ApplicationUser>(options) {
 public DbSet<RaceEvent> Events=>Set<RaceEvent>();
 public DbSet<Category> Categories=>Set<Category>();
 public DbSet<Enrolment> Enrolments=>Set<Enrolment>();
 public DbSet<ApiToken> Tokens=>Set<ApiToken>();
 protected override void OnModelCreating(ModelBuilder b) {
  base.OnModelCreating(b);
  b.Entity<ApiToken>().HasKey(x=>x.Hash);
  b.Entity<RaceEvent>().HasOne(x=>x.Organiser).WithMany().HasForeignKey(x=>x.OrganiserId).OnDelete(DeleteBehavior.Restrict);
  b.Entity<Enrolment>().HasOne(x=>x.Participant).WithMany().HasForeignKey(x=>x.ParticipantId).OnDelete(DeleteBehavior.Restrict);
  b.Entity<Enrolment>().HasOne(x=>x.Category).WithMany().HasForeignKey(x=>x.CategoryId).OnDelete(DeleteBehavior.Restrict);
  b.Entity<Enrolment>().HasIndex(x=>new{x.ParticipantId,x.EventId}).IsUnique();
  b.Entity<Category>().HasIndex(x=>new{x.EventId,x.Name}).IsUnique();
 }
}
