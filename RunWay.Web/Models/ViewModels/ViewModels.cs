using System.ComponentModel.DataAnnotations;
using RunWay.Web.Models.Api;

namespace RunWay.Web.Models.ViewModels;

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    public int? StatusCode { get; set; }
    public string? Message { get; set; }
}

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email address."), EmailAddress(ErrorMessage = "That does not look like a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password."), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "First name is required."), StringLength(50)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required."), StringLength(50)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required."), EmailAddress(ErrorMessage = "That does not look like a valid email address.")]
    public string Email { get; set; } = string.Empty;

    public Gender Gender { get; set; } = Gender.PreferNotToSay;

    [Display(Name = "I want to join as")]
    [Required]
    public string Role { get; set; } = RoleNames.Participant;

    [Required(ErrorMessage = "Password is required."), DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password), Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "Profile picture (optional)")]
    public IFormFile? ProfileImage { get; set; }
}

public class ProfileViewModel
{
    public string Email { get; set; } = string.Empty;
    public string? ProfileImage { get; set; }
    public List<string> Roles { get; set; } = new();

    [Required(ErrorMessage = "First name is required."), StringLength(50)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required."), StringLength(50)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    public Gender Gender { get; set; }

    [Phone(ErrorMessage = "That does not look like a valid phone number."), StringLength(30)]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }
}

public class EventFilterViewModel
{
    public string? Search { get; set; }
    public EventType? Type { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public decimal? MinDistance { get; set; }
    public decimal? MaxDistance { get; set; }

    public bool HasFilters => !string.IsNullOrWhiteSpace(Search) || Type.HasValue || From.HasValue
                              || To.HasValue || MinDistance.HasValue || MaxDistance.HasValue;
}

public class HomeViewModel
{
    public EventFilterViewModel Filter { get; set; } = new();
    public List<EventDto> Events { get; set; } = new();
}

public class EventDetailsViewModel
{
    public EventDto Event { get; set; } = new();
    public List<ResultDto> Results { get; set; } = new();
    public EnrolmentDto? MyEnrolment { get; set; }
}

public class EventFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Please give the event a name."), StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please describe the event."), StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a date and time.")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Date and time (South Africa)")]
    public DateTime? Date { get; set; }

    [Required(ErrorMessage = "Please enter the location."), StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the distance.")]
    [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Main distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Display(Name = "Event type")]
    public EventType EventType { get; set; }

    [Display(Name = "Banner image")]
    public IFormFile? Banner { get; set; }

    public string? ExistingBanner { get; set; }
}

public class CategoryFormViewModel
{
    public int? Id { get; set; }
    public int EventId { get; set; }

    [Required(ErrorMessage = "Please name the category."), StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the distance.")]
    [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }
}

public class CategoriesPageViewModel
{
    public EventDto Event { get; set; } = new();
    public CategoryFormViewModel Form { get; set; } = new();
}

public class EventEnrolmentsViewModel
{
    public EventDto Event { get; set; } = new();
    public List<EnrolmentDto> Enrolments { get; set; } = new();
}

public class ResultRowViewModel
{
    public int EnrolmentId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
}

public class ResultsPageViewModel
{
    public EventDto Event { get; set; } = new();
    public List<ResultRowViewModel> Rows { get; set; } = new();
}

public class UserRolesViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public IEnumerable<string> Roles { get; set; } = new List<string>();
}

public class ManageUserRolesViewModel
{
    public string RoleName { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}
