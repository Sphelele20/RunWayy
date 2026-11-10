using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RunWay.Web.Models.Api;
using RunWay.Web.Services;

namespace RunWay.Web.Controllers;

[Authorize(Roles = RoleNames.Participant)]
public class ResultsController : Controller
{
    private readonly RaceDayApiClient _api;

    public ResultsController(RaceDayApiClient api) => _api = api;

    /// <summary>"My Results": race history with finish time and position (e.g. 47th of 312).</summary>
    public async Task<IActionResult> Mine()
    {
        var result = await _api.GetMyResultsAsync();
        if (!result.Success) ViewBag.LoadError = result.Error;
        return View(result.Data ?? new List<ResultDto>());
    }
}
