using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RunWay.Web.Models.Api;
using RunWay.Web.Models.ViewModels;
using RunWay.Web.Services;

namespace RunWay.Web.Controllers;

[Authorize(Roles = RoleNames.Admin)]
public class UserRolesController : Controller
{
    private static readonly string[] AllRoles = { RoleNames.Admin, RoleNames.Organiser, RoleNames.Participant };

    private readonly RaceDayApiClient _api;

    public UserRolesController(RaceDayApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var result = await _api.GetUsersAsync();
        if (!result.Success) ViewBag.LoadError = result.Error;

        var model = (result.Data ?? new List<UserWithRolesDto>()).Select(u => new UserRolesViewModel
        {
            UserId = u.Id,
            Email = u.Email,
            FullName = u.FullName,
            Roles = u.Roles
        }).ToList();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Manage(string userId)
    {
        var result = await _api.GetUserAsync(userId);
        if (!result.Success || result.Data == null)
        {
            ViewBag.ErrorMessage = result.IsNotFound ? $"User with Id = {userId} cannot be found." : result.Error;
            return View("NotFound");
        }

        ViewBag.userId = userId;
        ViewBag.UserName = result.Data.Email;

        var model = AllRoles.Select(r => new ManageUserRolesViewModel
        {
            RoleName = r,
            IsSelected = result.Data.Roles.Contains(r)
        }).ToList();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Manage(List<ManageUserRolesViewModel> model, string userId)
    {
        var selected = (model ?? new()).Where(m => m.IsSelected).Select(m => m.RoleName).ToList();

        var result = await _api.SetUserRolesAsync(userId, selected);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Manage), new { userId });
        }

        TempData["Success"] = "Roles updated.";
        return RedirectToAction(nameof(Index));
    }
}
