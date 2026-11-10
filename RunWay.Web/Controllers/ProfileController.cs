using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using RunWay.Web.Models.Api;
using RunWay.Web.Models.ViewModels;
using RunWay.Web.Services;

namespace RunWay.Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly RaceDayApiClient _api;

    public ProfileController(RaceDayApiClient api) => _api = api;

    private static ProfileViewModel ToViewModel(ProfileDto p) => new()
    {
        Email = p.Email,
        FirstName = p.FirstName,
        LastName = p.LastName,
        Gender = p.Gender,
        PhoneNumber = p.PhoneNumber,
        ProfileImage = p.ProfileImage,
        Roles = p.Roles
    };

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await _api.GetProfileAsync();
        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction("Index", "Home");
        }
        return View(ToViewModel(result.Data));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(ProfileViewModel model)
    {
        if (!ModelState.IsValid) return View("Index", model);

        var result = await _api.UpdateProfileAsync(new UpdateProfileRequest
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Gender = model.Gender,
            PhoneNumber = model.PhoneNumber
        });

        if (!result.Success || result.Data == null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Your details could not be saved.");
            return View("Index", model);
        }

        await RefreshCookieAsync(result.Data);
        TempData["Success"] = "Your details have been updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        if (picture == null || picture.Length == 0)
        {
            TempData["Error"] = "Please choose an image first.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _api.UploadProfilePictureAsync(picture);
        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        await RefreshCookieAsync(result.Data);
        TempData["Success"] = "Profile picture updated.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Re-issues the auth cookie so the name and picture in the menu are current.</summary>
    private async Task RefreshCookieAsync(ProfileDto profile)
    {
        var principal = AccountController.BuildPrincipal(profile.Id, profile.Email,
            $"{profile.FirstName} {profile.LastName}", profile.ProfileImage, profile.Roles);

        var current = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = false, ExpiresUtc = current.Properties?.ExpiresUtc });
    }
}
