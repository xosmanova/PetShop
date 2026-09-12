using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PetShop.Areas.Admin.Models;
using PetShop.DataContext.Entites;

namespace PetShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [Route("admin/profile")]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: /admin/profile
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var roles = await _userManager.GetRolesAsync(user);

            var model = new AdminProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Bio = user.Bio,
                Department = string.IsNullOrWhiteSpace(user.Department) ? "Product Management" : user.Department,
                JobTitle = string.IsNullOrWhiteSpace(user.JobTitle) ? "Product Administrator" : user.JobTitle,
                TimeZone = string.IsNullOrWhiteSpace(user.TimeZone) ? "Baku (UTC+04:00)" : user.TimeZone,
                Roles = roles.ToList(),
                IsEmailConfirmed = user.EmailConfirmed
            };

            return View(model);
        }

        // POST: /admin/profile
        [HttpPost("")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(AdminProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var roles = await _userManager.GetRolesAsync(user);
            model.Roles = roles.ToList();
            model.IsEmailConfirmed = user.EmailConfirmed;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Email dəyişikliyi yoxlanışı
            if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    ModelState.AddModelError(nameof(model.Email), "Bu e-poçt ünvanı artıq başqa istifadəçi tərəfindən qeydiyyatdan keçirilib.");
                    return View(model);
                }

                user.Email = model.Email.Trim();
                user.UserName = model.Email.Trim();
            }

            user.FirstName = model.FirstName.Trim();
            user.LastName = model.LastName.Trim();
            user.PhoneNumber = model.PhoneNumber?.Trim();
            user.Bio = model.Bio?.Trim();
            user.Department = string.IsNullOrWhiteSpace(model.Department) ? "Product Management" : model.Department.Trim();
            user.JobTitle = string.IsNullOrWhiteSpace(model.JobTitle) ? "Product Administrator" : model.JobTitle.Trim();
            user.TimeZone = string.IsNullOrWhiteSpace(model.TimeZone) ? "Baku (UTC+04:00)" : model.TimeZone.Trim();

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            // Cari sessiyanı yenilə ki, yeni ad və məlumatlar header-də əks olunsun
            await _signInManager.RefreshSignInAsync(user);

            TempData["SuccessMessage"] = "Profil məlumatları uğurla yeniləndi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
