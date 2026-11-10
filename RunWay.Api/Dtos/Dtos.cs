using System.ComponentModel.DataAnnotations;
using RunWay.Api.Data;

namespace RunWay.Api.Dtos;

// ---------- Auth / profile ----------
public class RegisterRequest
{
    [Required, MaxLength(50)] public string FirstName { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string LastName { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 6)] public string Password { get; set; } = string.Empty;
    [EnumDataType(typeof(Gender))] public Gender Gender { get; set; } = Gender.PreferNotToSay;

    /// <summary>Participant or Organiser. Admin cannot be self-assigned.</summary>
    [Required] public string Role { get; set; } = Roles.Participant;
}

public class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? ProfileImage { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class ProfileDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    [EnumDataType(typeof(Gender))] public Gender Gender { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ProfileImage { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class UpdateProfileRequest
{
    [Required, MaxLength(50)] public string FirstName { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string LastName { get; set; } = string.Empty;
    [EnumDataType(typeof(Gender))] public Gender Gender { get; set; }
    [Phone, MaxLength(30)] public string? PhoneNumber { get; set; }
}

// ---------- Events / categories ----------
public class EventFilter
{
    public string? Search { get; set; }
    public EventType? EventType { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public decimal? MinDistance { get; set; }
    public decimal? MaxDistance { get; set; }
    public bool IncludePast { get; set; }
}

public class EventRequest
{
    [Required, MaxLength(150)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(2000)] public string Description { get; set; } = string.Empty;
    [Required] public DateTime Date { get; set; }
    [Required, MaxLength(200)] public string Location { get; set; } = string.Empty;
    [Range(0.1, 1000)] public decimal DistanceKm { get; set; }
    [EnumDataType(typeof(EventType))] public EventType EventType { get; set; }
}

public class CategoryRequest
{
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    [Range(0.1, 1000)] public decimal DistanceKm { get; set; }
}

public class CategoryDto
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public int EnrolmentCount { get; set; }
}

public class EventDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    [EnumDataType(typeof(EventType))] public EventType EventType { get; set; }
    public string? BannerImage { get; set; }
    public string OrganiserId { get; set; } = string.Empty;
    public string OrganiserName { get; set; } = string.Empty;
    public int EnrolmentCount { get; set; }
    public bool IsPast { get; set; }
    public List<CategoryDto> Categories { get; set; } = new();
}

// ---------- Enrolments / results ----------
public class EnrolRequest
{
    [Required] public int EventId { get; set; }
    [Required] public int CategoryId { get; set; }
}

public class UpdateEnrolmentStatusRequest
{
    [Required, EnumDataType(typeof(EnrolmentStatus))] public EnrolmentStatus Status { get; set; }
}

public class EnrolmentDto
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string EventLocation { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal CategoryDistanceKm { get; set; }
    public string ParticipantId { get; set; } = string.Empty;
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantEmail { get; set; } = string.Empty;
    public EnrolmentStatus Status { get; set; }
    public DateTime EnrolledAt { get; set; }
    public bool EventIsPast { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
}

public class ResultRequest
{
    [Required] public int EnrolmentId { get; set; }
    [Required] public TimeSpan FinishTime { get; set; }
    [Range(1, 100000)] public int Position { get; set; }
}

public class ResultDto
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string ParticipantName { get; set; } = string.Empty;
    public TimeSpan FinishTime { get; set; }
    public int Position { get; set; }
    public int TotalFinishers { get; set; }
}

// ---------- Dashboards / admin ----------
public class UpcomingEventDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Location { get; set; } = string.Empty;
    public int EnrolmentCount { get; set; }
}

public class OrganiserDashboardDto
{
    public int TotalEvents { get; set; }
    public int TotalEnrolments { get; set; }
    public int PendingEnrolments { get; set; }
    public List<UpcomingEventDto> UpcomingEvents { get; set; } = new();
}

public class AdminDashboardDto
{
    public int TotalUsers { get; set; }
    public int TotalEvents { get; set; }
    public int TotalEnrolments { get; set; }
    public Dictionary<string, int> UsersByRole { get; set; } = new();
}

public class UserWithRolesDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

public class UpdateRolesRequest
{
    public List<string> Roles { get; set; } = new();
}
