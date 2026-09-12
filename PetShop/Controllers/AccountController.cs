using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PetShop.DataContext;
using PetShop.DataContext.Entites;
using PetShop.Models;

namespace PetShop.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AppDbContext _context;
        private const string SessionKey = "BasketSessionId";

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Bu e-poçt ünvanı artıq istifadə olunur.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Member");
                await _signInManager.SignInAsync(user, isPersistent: false);
                await MigrateSessionCartAndWishlistAsync(user.Id);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [EnableRateLimiting("login_policy")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "E-poçt və ya şifrə yanlışdır.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                await MigrateSessionCartAndWishlistAsync(user.Id);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Admin"))
                {
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                }

                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Hesabınız müvəqqəti olaraq bloklanıb. Zəhmət olmasa bir az sonra cəhd edin.");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "E-poçt və ya şifrə yanlışdır.");
            }

            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Profile
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction(nameof(Login));

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction("Index", "Profile", new { area = "Admin" });
            }

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.ProductVariant)
                .Where(o => o.UserId == user.Id || (user.Email != null && o.Email == user.Email))
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync();

            var model = new ProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                City = user.City,
                Orders = orders
            };

            return View(model);
        }

        // POST: /Account/Profile
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction(nameof(Login));

            if (!ModelState.IsValid)
            {
                model.Orders = await _context.Orders
                    .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                    .Include(o => o.OrderItems).ThenInclude(oi => oi.ProductVariant)
                    .Where(o => o.UserId == user.Id || (user.Email != null && o.Email == user.Email))
                    .OrderByDescending(o => o.CreatedDate)
                    .ToListAsync();
                return View(model);
            }

            // E-poçt dəyişikliyi yoxlanışı
            if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
            {
                var existingEmailUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingEmailUser != null && existingEmailUser.Id != user.Id)
                {
                    ModelState.AddModelError("Email", "Bu e-poçt ünvanı artıq başqa istifadəçi tərəfindən istifadə olunur.");
                    model.Orders = await _context.Orders
                        .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                        .Include(o => o.OrderItems).ThenInclude(oi => oi.ProductVariant)
                        .Where(o => o.UserId == user.Id || (user.Email != null && o.Email == user.Email))
                        .OrderByDescending(o => o.CreatedDate)
                        .ToListAsync();
                    return View(model);
                }

                user.Email = model.Email;
                user.UserName = model.Email;
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;
            user.City = model.City;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                model.Orders = await _context.Orders
                    .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                    .Include(o => o.OrderItems).ThenInclude(oi => oi.ProductVariant)
                    .Where(o => o.UserId == user.Id || (user.Email != null && o.Email == user.Email))
                    .OrderByDescending(o => o.CreatedDate)
                    .ToListAsync();
                return View(model);
            }

            // Şifrə dəyişmə istəyi varsa
            if (!string.IsNullOrEmpty(model.CurrentPassword) && !string.IsNullOrEmpty(model.NewPassword))
            {
                var changePassResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                if (!changePassResult.Succeeded)
                {
                    foreach (var error in changePassResult.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);

                    model.Orders = await _context.Orders
                        .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                        .Include(o => o.OrderItems).ThenInclude(oi => oi.ProductVariant)
                        .Where(o => o.UserId == user.Id || (user.Email != null && o.Email == user.Email))
                        .OrderByDescending(o => o.CreatedDate)
                        .ToListAsync();
                    return View(model);
                }
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["Success"] = "Profil məlumatlarınız uğurla yeniləndi.";

            return RedirectToAction(nameof(Profile));
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private async Task MigrateSessionCartAndWishlistAsync(string userId)
        {
            var sessionId = HttpContext.Session.GetString(SessionKey);
            if (string.IsNullOrEmpty(sessionId))
                return;

            try
            {
                // 1. Səbəti bağla və ya birləşdir
                var guestBasket = await _context.Baskets
                    .Include(b => b.BasketItems)
                    .FirstOrDefaultAsync(b => b.SessionId == sessionId && b.UserId == null);

                var userBasket = await _context.Baskets
                    .Include(b => b.BasketItems)
                    .FirstOrDefaultAsync(b => b.UserId == userId);

                if (guestBasket != null)
                {
                    if (userBasket == null)
                    {
                        guestBasket.UserId = userId;
                    }
                    else
                    {
                        foreach (var guestItem in guestBasket.BasketItems.ToList())
                        {
                            var existingItem = userBasket.BasketItems
                                .FirstOrDefault(bi => bi.ProductId == guestItem.ProductId && bi.ProductVariantId == guestItem.ProductVariantId);

                            if (existingItem != null)
                            {
                                existingItem.Quantity += guestItem.Quantity;
                            }
                            else
                            {
                                userBasket.BasketItems.Add(new BasketItem
                                {
                                    BasketId = userBasket.Id,
                                    ProductId = guestItem.ProductId,
                                    ProductVariantId = guestItem.ProductVariantId,
                                    Quantity = guestItem.Quantity,
                                    UnitPrice = guestItem.UnitPrice
                                });
                            }
                        }
                        _context.Baskets.Remove(guestBasket);
                    }
                }

                // 2. Wishlist-i istifadəçiyə bağla
                var guestWishlist = await _context.Wishlist
                    .Where(w => w.SessionId == sessionId && w.UserId == null)
                    .ToListAsync();

                if (guestWishlist.Any())
                {
                    var userWishlistProductIds = await _context.Wishlist
                        .Where(w => w.UserId == userId)
                        .Select(w => w.ProductId)
                        .ToListAsync();

                    foreach (var item in guestWishlist)
                    {
                        if (userWishlistProductIds.Contains(item.ProductId))
                        {
                            _context.Wishlist.Remove(item);
                        }
                        else
                        {
                            item.UserId = userId;
                        }
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch
            {
                // Sessiyanın köçürülməsi zamanı xəta olarsa login prosesini dayandırmırıq
            }
        }
    }
}
