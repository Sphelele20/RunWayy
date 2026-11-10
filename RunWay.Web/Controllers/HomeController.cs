using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RunWay.Web.Models.Api;
using RunWay.Web.Models.ViewModels;
using RunWay.Web.Services;

namespace RunWay.Web.Controllers;

public class HomeController : Controller
{
    private readonly RaceDayApiClient _api;

    public HomeController(RaceDayApiClient api) => _api = api;

    /// <summary>Home page: all upcoming events with filtering.</summary>
    public async Task<IActionResult> Index([FromQuery] EventFilterViewModel filter)
    {
        if (filter.From.HasValue && filter.To.HasValue && filter.To < filter.From)
        {
            TempData["Warning"] = "The 'to' date was before the 'from' date, so the dates were ignored.";
            filter.From = null;
            filter.To = null;
        }
        if (filter.MinDistance.HasValue && filter.MaxDistance.HasValue && filter.MaxDistance < filter.MinDistance)
        {
            TempData["Warning"] = "The maximum distance was smaller than the minimum, so the distance filter was ignored.";
            filter.MinDistance = null;
            filter.MaxDistance = null;
        }

        var result = await _api.GetEventsAsync(filter.Search, filter.Type, filter.From, filter.To,
            filter.MinDistance, filter.MaxDistance);

        if (!result.Success) ViewBag.LoadError = result.Error;

        return View(new HomeViewModel { Filter = filter, Events = result.Data ?? new List<EventDto>() });
    }

    public IActionResult Privacy() => View();

    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> AdminDashboard()
    {
        var result = await _api.GetAdminDashboardAsync();
        if (!result.Success) ViewBag.LoadError = result.Error;
        return View(result.Data ?? new AdminDashboardDto());
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult HttpStatus(int code)
    {
        var message = code switch
        {
            404 => "We could not find that page.",
            403 => "You do not have permission to view that page.",
            _ => "Something went wrong."
        };
        Response.StatusCode = code;
        return View("Error", new ErrorViewModel { StatusCode = code, Message = message, RequestId = HttpContext.TraceIdentifier });
    }
}
