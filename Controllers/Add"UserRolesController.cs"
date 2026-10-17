using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RunWay.Data;
using RunWay.Models.ViewModels;

namespace RunWay.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserRolesController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserRolesController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
            var model = new List<UserRolesViewModel>();

            foreach (var user in users)
            {
                model.Add(new UserRolesViewModel
                {
                    UserId = user.Id,
                    Email = user.Email,
                    UserName = user.UserName,
                    Roles = await _userManager.GetRolesAsync(user)
                });
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Manage(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"User with Id = {userId} cannot be found.";
                return View("NotFound");
            }

            ViewBag.userId = userId;
            ViewBag.UserName = user.UserName;

            var model = new List<ManageUserRolesViewModel>();
            foreach (var role in await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync())
            {
                model.Add(new ManageUserRolesViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name ?? string.Empty,
                    isSelected = await _userManager.IsInRoleAsync(user, role.Name!)
                });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(List<ManageUserRolesViewModel> model, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"User with Id = {userId} cannot be found.";
                return View("NotFound");
            }

            var selected = (model ?? new()).Where(m => m.isSelected).Select(m => m.RoleName).ToList();

            // Don't let an admin lock themselves out.
            if (user.Id == _userManager.GetUserId(User) && !selected.Contains("Admin"))
            {
                selected.Add("Admin");
            }

            var current = await _userManager.GetRolesAsync(user);

            var toRemove = current.Except(selected).ToList();
            if (toRemove.Count > 0)
            {
                var removed = await _userManager.RemoveFromRolesAsync(user, toRemove);
                if (!removed.Succeeded)
                {
                    ModelState.AddModelError("", "Cannot remove user's existing roles.");
                    return await Manage(userId);
                }
            }

            var toAdd = selected.Except(current).ToList();
            if (toAdd.Count > 0)
            {
                var added = await _userManager.AddToRolesAsync(user, toAdd);
                if (!added.Succeeded)
                {
                    ModelState.AddModelError("", "Cannot add selected roles to user.");
                    return await Manage(userId);
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
