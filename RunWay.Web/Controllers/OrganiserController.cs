using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RunWay.Web.Models.Api;
using RunWay.Web.Models.ViewModels;
using RunWay.Web.Services;

namespace RunWay.Web.Controllers;

/// <summary>Organiser interface: dashboard, events, categories, enrolments and results.</summary>
[Authorize(Roles = RoleNames.Organiser)]
public class OrganiserController : Controller
{
    private readonly RaceDayApiClient _api;

    public OrganiserController(RaceDayApiClient api) => _api = api;

    // ---------------------------------------------------------------- dashboard

    public async Task<IActionResult> Dashboard()
    {
        var result = await _api.GetOrganiserDashboardAsync();
        if (!result.Success) ViewBag.LoadError = result.Error;
        return View(result.Data ?? new OrganiserDashboardDto());
    }

    // ---------------------------------------------------------------- events

    public async Task<IActionResult> Events()
    {
        var result = await _api.GetMyEventsAsync();
        if (!result.Success) ViewBag.LoadError = result.Error;
        return View(result.Data ?? new List<EventDto>());
    }

    [HttpGet]
    public IActionResult CreateEvent() =>
        View("EventForm", new EventFormViewModel { Date = RunWay.Web.Models.RaceClock.Today.AddDays(30).AddHours(8) });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(EventFormViewModel model)
    {
        ValidateEventDate(model, isNew: true);
        if (!ModelState.IsValid) return View("EventForm", model);

        var created = await _api.CreateEventAsync(ToRequest(model));
        if (!created.Success || created.Data == null)
        {
            ModelState.AddModelError(string.Empty, created.Error ?? "The event could not be created.");
            return View("EventForm", model);
        }

        if (model.Banner is { Length: > 0 })
        {
            var banner = await _api.UploadBannerAsync(created.Data.Id, model.Banner);
            if (!banner.Success)
                TempData["Warning"] = $"The event was created, but the banner was not saved: {banner.Error}";
        }

        TempData["Success"] = "Event created. Now add the distance categories participants can enrol in.";
        return RedirectToAction(nameof(Categories), new { eventId = created.Data.Id });
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var result = await _api.GetEventAsync(id);
        if (!result.Success || result.Data == null) return EventNotAvailable(result);

        var e = result.Data;
        return View("EventForm", new EventFormViewModel
        {
            Id = e.Id,
            Name = e.Name,
            Description = e.Description,
            Date = e.Date,
            Location = e.Location,
            DistanceKm = e.DistanceKm,
            EventType = e.EventType,
            ExistingBanner = e.BannerImage
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(int id, EventFormViewModel model)
    {
        model.Id = id;
        ValidateEventDate(model, isNew: false);
        if (!ModelState.IsValid) return View("EventForm", model);

        var updated = await _api.UpdateEventAsync(id, ToRequest(model));
        if (!updated.Success || updated.Data == null)
        {
            ModelState.AddModelError(string.Empty, updated.Error ?? "The event could not be saved.");
            model.ExistingBanner ??= updated.Data?.BannerImage;
            return View("EventForm", model);
        }

        if (model.Banner is { Length: > 0 })
        {
            var banner = await _api.UploadBannerAsync(id, model.Banner);
            if (!banner.Success)
                TempData["Warning"] = $"The event was saved, but the banner was not: {banner.Error}";
        }

        TempData["Success"] = "Event updated.";
        return RedirectToAction(nameof(Events));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var result = await _api.DeleteEventAsync(id);
        if (result.Success) TempData["Success"] = "Event deleted.";
        else TempData["Error"] = result.Error;
        return RedirectToAction(nameof(Events));
    }

    // ---------------------------------------------------------------- categories

    [HttpGet]
    public async Task<IActionResult> Categories(int eventId, int? editId = null)
    {
        var ev = await _api.GetEventAsync(eventId);
        if (!ev.Success || ev.Data == null) return EventNotAvailable(ev);

        var model = new CategoriesPageViewModel { Event = ev.Data, Form = new CategoryFormViewModel { EventId = eventId } };
        var editing = editId.HasValue ? ev.Data.Categories.FirstOrDefault(c => c.Id == editId.Value) : null;
        if (editing != null)
            model.Form = new CategoryFormViewModel { Id = editing.Id, EventId = eventId, Name = editing.Name, DistanceKm = editing.DistanceKm };

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCategory(CategoryFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            var ev = await _api.GetEventAsync(form.EventId);
            if (!ev.Success || ev.Data == null) return EventNotAvailable(ev);
            return View("Categories", new CategoriesPageViewModel { Event = ev.Data, Form = form });
        }

        var request = new CategoryRequest { Name = form.Name, DistanceKm = form.DistanceKm!.Value };
        var result = form.Id.HasValue
            ? await _api.UpdateCategoryAsync(form.Id.Value, request)
            : await _api.CreateCategoryAsync(form.EventId, request);

        if (result.Success) TempData["Success"] = form.Id.HasValue ? "Category updated." : "Category added.";
        else TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Categories), new { eventId = form.EventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id, int eventId)
    {
        var result = await _api.DeleteCategoryAsync(id);
        if (result.Success) TempData["Success"] = "Category deleted.";
        else TempData["Error"] = result.Error;
        return RedirectToAction(nameof(Categories), new { eventId });
    }

    // ---------------------------------------------------------------- enrolments

    public async Task<IActionResult> Enrolments(int eventId)
    {
        var ev = await _api.GetEventAsync(eventId);
        if (!ev.Success || ev.Data == null) return EventNotAvailable(ev);

        var list = await _api.GetEventEnrolmentsAsync(eventId);
        if (!list.Success) ViewBag.LoadError = list.Error;

        return View(new EventEnrolmentsViewModel { Event = ev.Data, Enrolments = list.Data ?? new List<EnrolmentDto>() });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetEnrolmentStatus(int id, int eventId, EnrolmentStatus status)
    {
        var result = await _api.SetEnrolmentStatusAsync(id, status);
        if (result.Success) TempData["Success"] = $"Enrolment marked as {status}.";
        else TempData["Error"] = result.Error;
        return RedirectToAction(nameof(Enrolments), new { eventId });
    }

    // ---------------------------------------------------------------- results

    [HttpGet]
    public async Task<IActionResult> Results(int eventId)
    {
        var ev = await _api.GetEventAsync(eventId);
        if (!ev.Success || ev.Data == null) return EventNotAvailable(ev);

        var enrolments = await _api.GetEventEnrolmentsAsync(eventId);
        if (!enrolments.Success) ViewBag.LoadError = enrolments.Error;

        var rows = (enrolments.Data ?? new List<EnrolmentDto>())
            .Where(e => e.Status == EnrolmentStatus.Confirmed)
            .OrderBy(e => e.Position ?? int.MaxValue).ThenBy(e => e.ParticipantName)
            .Select(e => new ResultRowViewModel
            {
                EnrolmentId = e.Id,
                ParticipantName = e.ParticipantName,
                CategoryName = e.CategoryName,
                FinishTime = e.FinishTime,
                Position = e.Position
            }).ToList();

        return View(new ResultsPageViewModel { Event = ev.Data, Rows = rows });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveResult(int eventId, int enrolmentId, string? finishTime, int? position)
    {
        if (!TimeSpan.TryParse(finishTime, out var time) || time <= TimeSpan.Zero)
        {
            TempData["Error"] = "Enter the finish time as hh:mm:ss, for example 00:52:31.";
            return RedirectToAction(nameof(Results), new { eventId });
        }
        if (position is null or < 1)
        {
            TempData["Error"] = "Enter a finishing position of 1 or higher.";
            return RedirectToAction(nameof(Results), new { eventId });
        }

        var result = await _api.SaveResultAsync(new ResultRequest
        {
            EnrolmentId = enrolmentId,
            FinishTime = time,
            Position = position.Value
        });

        if (result.Success) TempData["Success"] = "Result saved and published.";
        else TempData["Error"] = result.Error;
        return RedirectToAction(nameof(Results), new { eventId });
    }

    // ---------------------------------------------------------------- helpers

    private void ValidateEventDate(EventFormViewModel model, bool isNew)
    {
        if (isNew && model.Date.HasValue && model.Date.Value < RunWay.Web.Models.RaceClock.Now)
            ModelState.AddModelError(nameof(model.Date), "A new event must be in the future.");

        if (model.Banner is { Length: > 5 * 1024 * 1024 })
            ModelState.AddModelError(nameof(model.Banner), "The banner must be 5 MB or smaller.");

        if (model.Banner is { Length: > 0 })
        {
            var ext = Path.GetExtension(model.Banner.FileName).ToLowerInvariant();
            if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
                ModelState.AddModelError(nameof(model.Banner), "Only .jpg, .jpeg, .png and .webp images are allowed.");
        }
    }

    private static EventRequest ToRequest(EventFormViewModel m) => new()
    {
        Name = m.Name,
        Description = m.Description,
        Date = m.Date!.Value,
        Location = m.Location,
        DistanceKm = m.DistanceKm!.Value,
        EventType = m.EventType
    };

    private IActionResult EventNotAvailable<T>(ApiResult<T> result)
    {
        TempData["Error"] = result.IsNotFound ? "That event could not be found." : result.Error;
        return RedirectToAction(nameof(Events));
    }
}
