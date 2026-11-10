namespace RunWay.Web.Models.Api;

// These classes mirror the JSON contracts of the RunWay.Api project.

public enum Gender { Male, Female, Other, PreferNotToSay }
public enum EventType { Road, Trail, FunRun, Marathon, Relay }
public enum EnrolmentStatus { Pending, Confirmed, Cancelled }

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

public class RegisterRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public string Role { get; set; } = RoleNames.Participant;
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
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
    public Gender Gender { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ProfileImage { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class UpdateProfileRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public string? PhoneNumber { get; set; }
}

public class CategoryDto
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public int EnrolmentCount { get; set; }
}

public class CategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
}

public class EventDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public EventType EventType { get; set; }
    public string? BannerImage { get; set; }
    public string OrganiserId { get; set; } = string.Empty;
    public string OrganiserName { get; set; } = string.Empty;
    public int EnrolmentCount { get; set; }
    public bool IsPast { get; set; }
    public List<CategoryDto> Categories { get; set; } = new();
}

public class EventRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public EventType EventType { get; set; }
}

public class EnrolRequest
{
    public int EventId { get; set; }
    public int CategoryId { get; set; }
}

public class UpdateEnrolmentStatusRequest
{
    public EnrolmentStatus Status { get; set; }
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
    public int EnrolmentId { get; set; }
    public TimeSpan FinishTime { get; set; }
    public int Position { get; set; }
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
