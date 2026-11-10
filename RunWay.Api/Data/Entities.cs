using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace RunWay.Api.Data;

public enum Gender { Male, Female, Other, PreferNotToSay }

public enum EventType { Road, Trail, FunRun, Marathon, Relay }

public enum EnrolmentStatus { Pending, Confirmed, Cancelled }

public static class Roles
{
    public const string Admin = "Admin";
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";

    public static readonly string[] All = { Admin, Organiser, Participant };
}

public class ApplicationUser : IdentityUser
{
    [Required, MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    public Gender Gender { get; set; }

    /// <summary>Blob name of the profile picture, e.g. "profiles/abc.jpg".</summary>
    [MaxLength(260)]
    public string? ProfileImagePath { get; set; }

    public string FullName => $"{FirstName} {LastName}";
}

public class Event
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    [Required, MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    public decimal DistanceKm { get; set; }

    public EventType EventType { get; set; }

    /// <summary>Blob name of the banner image, e.g. "events/abc.jpg".</summary>
    [MaxLength(260)]
    public string? BannerBlobName { get; set; }

    [Required]
    public string OrganiserId { get; set; } = string.Empty;
    public ApplicationUser? Organiser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Category> Categories { get; set; } = new();
    public List<Enrolment> Enrolments { get; set; } = new();
}

public class Category
{
    public int Id { get; set; }

    public int EventId { get; set; }
    public Event? Event { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public decimal DistanceKm { get; set; }

    public List<Enrolment> Enrolments { get; set; } = new();
}

public class Enrolment
{
    public int Id { get; set; }

    public int EventId { get; set; }
    public Event? Event { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    [Required]
    public string ParticipantId { get; set; } = string.Empty;
    public ApplicationUser? Participant { get; set; }

    public EnrolmentStatus Status { get; set; } = EnrolmentStatus.Pending;

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    public RaceResult? Result { get; set; }
}

public class RaceResult {
    public int EventId { get; set; }
    public int Id { get; set; }

    public int EnrolmentId { get; set; }
    public Enrolment? Enrolment { get; set; }

    public TimeSpan FinishTime { get; set; }

    /// <summary>Overall finishing position in the event (1 = winner).</summary>
    public int Position { get; set; }

    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
}
