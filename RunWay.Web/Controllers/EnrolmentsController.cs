using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RunWay.Web.Models.Api;
using RunWay.Web.Services;

namespace RunWay.Web.Controllers;

/// <summary>Participant pages: enrol in an event, see my enrolments, cancel, and see my race results.</summary>
[Authorize(Roles = RoleNames.Participant)]
public class EnrolmentsController : Controller
{
    private readonly RaceDayApiClient _api;

    public EnrolmentsController(RaceDayApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var result = await _api.GetMyEnrolmentsAsync();
        if (!result.Success) ViewBag.LoadError = result.Error;
        return View(result.Data ?? new List<EnrolmentDto>());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int eventId, int? categoryId)
    {
        if (categoryId is null or <= 0)
        {
            TempData["Error"] = "Please choose a category before enrolling.";
            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        var result = await _api.EnrolAsync(new EnrolRequest { EventId = eventId, CategoryId = categoryId.Value });
        if (result.Success)
            TempData["Success"] = $"You are enrolled in {result.Data?.EventName}. Your status is Pending until the organiser confirms it.";
        else
            TempData["Error"] = result.Error;

        return result.Success
            ? RedirectToAction(nameof(Index))
            : RedirectToAction("Details", "Events", new { id = eventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _api.CancelEnrolmentAsync(id);
        if (result.Success) TempData["Success"] = "Your enrolment has been cancelled.";
        else TempData["Error"] = result.Error;
        return RedirectToAction(nameof(Index));
    }
}
