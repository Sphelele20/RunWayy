using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RunWay.Web.Models.Api;
using RunWay.Web.Models.ViewModels;
using RunWay.Web.Services;

namespace RunWay.Web.Controllers;

public class EventsController : Controller
{
    private readonly RaceDayApiClient _api;

    public EventsController(RaceDayApiClient api) => _api = api;

    /// <summary>Event page: full information, categories, enrolment form (participants) and results.</summary>
    public async Task<IActionResult> Details(int id)
    {
        var ev = await _api.GetEventAsync(id);
        if (ev.IsNotFound) return NotFound();
        if (!ev.Success || ev.Data == null)
        {
            TempData["Error"] = ev.Error;
            return RedirectToAction("Index", "Home");
        }

        var model = new EventDetailsViewModel { Event = ev.Data };

        if (ev.Data.IsPast)
        {
            var results = await _api.GetEventResultsAsync(id);
            model.Results = results.Data ?? new List<ResultDto>();
        }

        if (User.IsInRole(RoleNames.Participant))
        {
            var mine = await _api.GetMyEnrolmentsAsync();
            model.MyEnrolment = mine.Data?.FirstOrDefault(e => e.EventId == id && e.Status != EnrolmentStatus.Cancelled);
        }

        return View(model);
    }
}
