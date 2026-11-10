using System.Security.Claims;
using RunWay.Api.Data;
using RunWay.Api.Dtos;

namespace RunWay.Api.Services;

public static class Mapping
{
    public static string? GetUserId(this ClaimsPrincipal user) => user.FindFirstValue("sub");

    /// <summary>The event can be managed by its organiser or by an Admin.</summary>
    public static bool CanManage(this ClaimsPrincipal user, Event ev) =>
        user.IsInRole(Roles.Admin) || ev.OrganiserId == user.GetUserId();

    public static EventDto ToDto(this Event e, bool includeCategories = false) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Description = e.Description,
        Date = e.Date,
        Location = e.Location,
        DistanceKm = e.DistanceKm,
        EventType = e.EventType,
        BannerImage = e.BannerBlobName,
        OrganiserId = e.OrganiserId,
        OrganiserName = e.Organiser?.FullName ?? string.Empty,
        EnrolmentCount = e.Enrolments.Count(x => x.Status != EnrolmentStatus.Cancelled),
        IsPast = e.Date < RunWay.Api.Services.RaceClock.Now,
        Categories = includeCategories
            ? e.Categories.OrderBy(c => c.DistanceKm).Select(c => c.ToDto()).ToList()
            : new List<CategoryDto>()
    };

    public static CategoryDto ToDto(this Category c) => new()
    {
        Id = c.Id,
        EventId = c.EventId,
        Name = c.Name,
        DistanceKm = c.DistanceKm,
        EnrolmentCount = c.Enrolments.Count(x => x.Status != EnrolmentStatus.Cancelled)
    };

    /// <summary>Requires Event, Category, Participant (and optionally Result) to be loaded.</summary>
    public static EnrolmentDto ToDto(this Enrolment e) => new()
    {
        Id = e.Id,
        EventId = e.EventId,
        EventName = e.Event?.Name ?? string.Empty,
        EventDate = e.Event?.Date ?? default,
        EventLocation = e.Event?.Location ?? string.Empty,
        CategoryId = e.CategoryId,
        CategoryName = e.Category?.Name ?? string.Empty,
        CategoryDistanceKm = e.Category?.DistanceKm ?? 0,
        ParticipantId = e.ParticipantId,
        ParticipantName = e.Participant?.FullName ?? string.Empty,
        ParticipantEmail = e.Participant?.Email ?? string.Empty,
        Status = e.Status,
        EnrolledAt = e.EnrolledAt,
        EventIsPast = e.Event != null && e.Event.Date < RunWay.Api.Services.RaceClock.Now,
        FinishTime = e.Result?.FinishTime,
        Position = e.Result?.Position
    };
}
