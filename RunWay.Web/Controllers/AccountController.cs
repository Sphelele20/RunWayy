using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using RunWay.Web.Models.Api;
using RunWay.Web.Models.ViewModels;
using RunWay.Web.Services;

namespace RunWay.Web.Controllers;

public class AccountController : Controller
{
    public const string ProfileImageClaim = "profile_image";

    private readonly RaceDayApiClient _api;

    public AccountController(RaceDayApiClient api) => _api = api;

    // ---------------------------------------------------------------- login

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _api.LoginAsync(new LoginRequest { Email = model.Email, Password = model.Password });
        if (!result.Success || result.Data == null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Login failed.");
            return View(model);
        }

        await SignInAsync(result.Data);
        TempData["Success"] = $"Welcome back, {result.Data.FullName}!";

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);
        return RedirectToHome(result.Data.Roles);
    }

    // ---------------------------------------------------------------- register

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View(new RegisterViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (model.Role != RoleNames.Participant && model.Role != RoleNames.Organiser)
        {
            ModelState.AddModelError(nameof(model.Role), "Please choose Participant or Organiser.");
            return View(model);
        }

        var result = await _api.RegisterAsync(new RegisterRequest
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            Password = model.Password,
            Gender = model.Gender,
            Role = model.Role
        });

        if (!result.Success || result.Data == null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Registration failed.");
            return View(model);
        }

        // Store the JWT first so the picture upload below is authenticated.
        HttpContext.Session.SetString(BearerTokenHandler.SessionKey, result.Data.Token);

        var auth = result.Data;
        if (model.ProfileImage is { Length: > 0 })
        {
            var upload = await _api.UploadProfilePictureAsync(model.ProfileImage);
            if (upload.Success && upload.Data != null)
                auth.ProfileImage = upload.Data.ProfileImage;
            else
                TempData["Warning"] = $"Your account was created, but the picture was not saved: {upload.Error}";
        }

        await SignInAsync(auth);
        TempData["Success"] = "Welcome to RaceDay! Your account is ready.";
        return RedirectToHome(auth.Roles);
    }

    // ---------------------------------------------------------------- logout / denied

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _api.LogoutAsync();
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Success"] = "You have been logged out.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    // ---------------------------------------------------------------- helpers

    private IActionResult RedirectToHome(IEnumerable<string> roles)
    {
        var list = roles.ToList();
        if (list.Contains(RoleNames.Admin)) return RedirectToAction("AdminDashboard", "Home");
        if (list.Contains(RoleNames.Organiser)) return RedirectToAction("Dashboard", "Organiser");
        return RedirectToAction("Index", "Home");
    }

    /// <summary>Keeps the JWT in server-side session and the user's identity + roles in the auth cookie.</summary>
    private async Task SignInAsync(AuthResponse auth)
    {
        HttpContext.Session.SetString(BearerTokenHandler.SessionKey, auth.Token);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            BuildPrincipal(auth.UserId, auth.Email, auth.FullName, auth.ProfileImage, auth.Roles),
            new AuthenticationProperties { IsPersistent = false, ExpiresUtc = auth.ExpiresAt });
    }

    public static ClaimsPrincipal BuildPrincipal(string userId, string email, string fullName, string? image, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, fullName),
            new(ClaimTypes.Email, email)
        };
        if (!string.IsNullOrEmpty(image)) claims.Add(new Claim(ProfileImageClaim, image));
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }
}
