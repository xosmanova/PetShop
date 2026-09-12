using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.DataContext;
using PetShop.DataContext.Entites;

namespace PetShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CouponsController : Controller
    {
        private readonly AppDbContext _context;

        public CouponsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Coupons
        public async Task<IActionResult> Index(string? search, string? status, int page = 1)
        {
            var query = _context.Coupons.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToUpper();
                query = query.Where(c => c.Code.Contains(term));
            }

            var now = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(status))
            {
                query = status switch
                {
                    "active" => query.Where(c => c.IsActive && (!c.ExpiryDate.HasValue || c.ExpiryDate.Value >= now)),
                    "inactive" => query.Where(c => !c.IsActive),
                    "expired" => query.Where(c => c.ExpiryDate.HasValue && c.ExpiryDate.Value < now),
                    _ => query
                };
            }

            int pageSize = 10;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            page = Math.Clamp(page, 1, Math.Max(1, totalPages));

            var coupons = await query
                .OrderByDescending(c => c.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(coupons);
        }

        // GET: /Admin/Coupons/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Coupon { IsActive = true, ExpiryDate = DateTime.Today.AddMonths(1) });
        }

        // POST: /Admin/Coupons/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Coupon coupon)
        {
            coupon.Code = coupon.Code?.Trim().ToUpper() ?? string.Empty;

            if (await _context.Coupons.AnyAsync(c => c.Code == coupon.Code))
            {
                ModelState.AddModelError("Code", $"'{coupon.Code}' kupon kodu artıq mövcuddur.");
            }

            if (!ModelState.IsValid)
            {
                return View(coupon);
            }

            coupon.CreatedDate = DateTime.UtcNow;
            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"'{coupon.Code}' kuponu uğurla yaradıldı.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Coupons/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
            {
                return NotFound();
            }

            return View(coupon);
        }

        // POST: /Admin/Coupons/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Coupon coupon)
        {
            if (id != coupon.Id)
            {
                return BadRequest();
            }

            coupon.Code = coupon.Code?.Trim().ToUpper() ?? string.Empty;

            if (await _context.Coupons.AnyAsync(c => c.Code == coupon.Code && c.Id != id))
            {
                ModelState.AddModelError("Code", $"'{coupon.Code}' kupon kodu artıq başqa kuponda istifadə olunur.");
            }

            if (!ModelState.IsValid)
            {
                return View(coupon);
            }

            var existing = await _context.Coupons.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.Code = coupon.Code;
            existing.DiscountType = coupon.DiscountType;
            existing.DiscountValue = coupon.DiscountValue;
            existing.ExpiryDate = coupon.ExpiryDate;
            existing.MinOrderAmount = coupon.MinOrderAmount;
            existing.IsActive = coupon.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"'{existing.Code}' kuponu uğurla yeniləndi.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Coupons/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
            {
                return NotFound();
            }

            coupon.IsActive = !coupon.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"'{coupon.Code}' kuponunun statusu '{(coupon.IsActive ? "Aktiv" : "Deaktiv")}' olaraq dəyişdirildi.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Coupons/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
            {
                return NotFound();
            }

            _context.Coupons.Remove(coupon);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"'{coupon.Code}' kuponu uğurla silindi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
