using System.ComponentModel.DataAnnotations;
namespace RunWay.Contracts;
public class LoginRequest {
 [Required, EmailAddress] public string Email {get;set;}="";
 [Required, StringLength(100, MinimumLength=8)] public string Password {get;set;}="";
}
public class RegisterRequest : LoginRequest {
 [Required,StringLength(50)] public string FirstName {get;set;}="";
 [Required,StringLength(50)] public string LastName {get;set;}="";
 [Required,RegularExpression("Male|Female|Other|PreferNotToSay")] public string Gender {get;set;}="PreferNotToSay";
 [Required,RegularExpression("Organiser|Participant")] public string Role {get;set;}="Participant";
}
public record AuthResponse(string Token, DateTime ExpiresAt, ProfileDto User);
public class ProfileDto {
 public string Id {get;set;}="";
 public string Email {get;set;}="";
 public string Role {get;set;}="";
 public string FirstName {get;set;}="";
 public string LastName {get;set;}="";
 public string Gender {get;set;}="";
 public string? PhoneNumber {get;set;}
 public string? ImageUrl {get;set;}
 public string FullName => FirstName+" "+LastName;
}
public class ProfileRequest {
 [Required,StringLength(50)] public string FirstName {get;set;}="";
 [Required,StringLength(50)] public string LastName {get;set;}="";
 [Required,RegularExpression("Male|Female|Other|PreferNotToSay")] public string Gender {get;set;}="PreferNotToSay";
 [Phone] public string? PhoneNumber {get;set;}
}
public class EventRequest {
 public int Id {get;set;}
 [Required,StringLength(200)] public string Name {get;set;}="";
 [Required,StringLength(4000)] public string Description {get;set;}="";
 [Required,StringLength(255)] public string Location {get;set;}="";
 public DateTime Date {get;set;}=DateTime.UtcNow.AddDays(7);
 [Range(0.1,1000)] public double Distance {get;set;}=5;
 [Required,RegularExpression("Running|Walking|Cycling")] public string EventType {get;set;}="Running";
}
public class EventDto : EventRequest {
 public string OrganiserId {get;set;}="";
 public string? BannerUrl {get;set;}
 public List<CategoryDto> Categories {get;set;}=[];
 public int EnrolmentCount {get;set;}
}
public class CategoryRequest {
 [Required,StringLength(100)] public string Name {get;set;}="";
 [StringLength(255)] public string? Description {get;set;}
}
public class CategoryDto : CategoryRequest { public int Id {get;set;} }
public record EnrolRequest(int CategoryId);
public class ResultRequest {
 [Required,RegularExpression(@"\d{2,3}:[0-5]\d:[0-5]\d",ErrorMessage="Use HH:mm:ss, for example 01:32:18.")] public string FinishTime {get;set;}="";
 [Range(1,int.MaxValue)] public int Position {get;set;}=1;
}
public class EnrolmentDto {
 public int Id {get;set;}
 public int EventId {get;set;}
 public string EventName {get;set;}="";
 public DateTime Date {get;set;}
 public string EventType {get;set;}="";
 public string ParticipantName {get;set;}="";
 public string Category {get;set;}="";
 public string Status {get;set;}="";
 public string? FinishTime {get;set;}
 public int? Position {get;set;}
}
