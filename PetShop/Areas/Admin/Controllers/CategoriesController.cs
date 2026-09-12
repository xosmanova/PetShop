using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PetShop.Areas.Admin.Models;
using PetShop.DataContext;
using PetShop.DataContext.Entites;
using System.Text.RegularExpressions;

namespace PetShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Categories
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.ProductCategories
                .Include(c => c.ParentCategory)
                .Include(c => c.SubCategories)
                .Include(c => c.Products)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.Trim().ToLower();
                query = query.Where(c =>
                    c.Name.ToLower().Contains(lowerSearch) ||
                    c.Slug.ToLower().Contains(lowerSearch) ||
                    (c.ParentCategory != null && c.ParentCategory.Name.ToLower().Contains(lowerSearch)));
            }

            var categories = await query.OrderBy(c => c.ParentCategoryId.HasValue)
                                        .ThenBy(c => c.Name)
                                        .ToListAsync();

            ViewBag.Search = search;
            return View(categories);
        }

        // GET: Admin/Categories/Create
        public async Task<IActionResult> Create()
        {
            await PopulateParentCategoriesAsync();
            return View(new CategoryFormViewModel());
        }

        // POST: Admin/Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryFormViewModel model)
        {
            // Slug boşdursa, Name-dən avtomatik generasiya et
            if (string.IsNullOrWhiteSpace(model.Slug))
            {
                model.Slug = GenerateSlug(model.Name);
            }
            else
            {
                model.Slug = GenerateSlug(model.Slug);
            }

            // Slug unikal olmalıdır
            if (await _context.ProductCategories.AnyAsync(c => c.Slug == model.Slug))
            {
                ModelState.AddModelError("Slug", "Bu slug (URL adı) artıq başqa bir kateqoriya tərəfindən istifadə olunur.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateParentCategoriesAsync(model.ParentCategoryId);
                return View(model);
            }

            var category = new ProductCategory
            {
                Name = model.Name.Trim(),
                Slug = model.Slug,
                IconClass = string.IsNullOrWhiteSpace(model.IconClass) ? null : model.IconClass.Trim(),
                ParentCategoryId = model.ParentCategoryId
            };

            _context.ProductCategories.Add(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"'{category.Name}' kateqoriyası uğurla əlavə edildi!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Categories/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.ProductCategories.FindAsync(id);
            if (category == null)
            {
                TempData["Error"] = "Kateqoriya tapılmadı!";
                return RedirectToAction(nameof(Index));
            }

            var model = new CategoryFormViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                IconClass = category.IconClass,
                ParentCategoryId = category.ParentCategoryId
            };

            await PopulateParentCategoriesAsync(category.ParentCategoryId, category.Id);
            return View(model);
        }

        // POST: Admin/Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoryFormViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(model.Slug))
            {
                model.Slug = GenerateSlug(model.Name);
            }
            else
            {
                model.Slug = GenerateSlug(model.Slug);
            }

            // Kateqoriya öz-özünə parent ola bilməz
            if (model.ParentCategoryId.HasValue && model.ParentCategoryId.Value == id)
            {
                ModelState.AddModelError("ParentCategoryId", "Kateqoriya öz-özünün ana kateqoriyası ola bilməz.");
            }

            // Slug unikal olmalıdır (cari kateqoriya istisna)
            if (await _context.ProductCategories.AnyAsync(c => c.Slug == model.Slug && c.Id != id))
            {
                ModelState.AddModelError("Slug", "Bu slug (URL adı) artıq başqa bir kateqoriya tərəfindən istifadə olunur.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateParentCategoriesAsync(model.ParentCategoryId, id);
                return View(model);
            }

            var category = await _context.ProductCategories.FindAsync(id);
            if (category == null)
            {
                TempData["Error"] = "Redaktə ediləcək kateqoriya tapılmadı!";
                return RedirectToAction(nameof(Index));
            }

            category.Name = model.Name.Trim();
            category.Slug = model.Slug;
            category.IconClass = string.IsNullOrWhiteSpace(model.IconClass) ? null : model.IconClass.Trim();
            category.ParentCategoryId = model.ParentCategoryId;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"'{category.Name}' kateqoriyası uğurla yeniləndi!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Categories/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.ProductCategories
                .Include(c => c.Products)
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                TempData["Error"] = "Silinəcək kateqoriya tapılmadı!";
                return RedirectToAction(nameof(Index));
            }

            // 1. Əgər həmin kateqoriyaya bağlı Product varsa, xəbərdarlıq göstər və silməyə icazə vermə
            int productCount = await _context.Products.CountAsync(p => p.ProductCategoryId == id);
            if (productCount > 0)
            {
                TempData["Warning"] = $"'{category.Name}' kateqoriyasına bağlı {productCount} ədəd məhsul mövcuddur! Məhsullar olan kateqoriyanı silmək mümkün deyil. Əvvəlcə həmin məhsulların kateqoriyasını dəyişməli və ya onları silməlisiniz.";
                return RedirectToAction(nameof(Index));
            }

            // 2. Alt kateqoriyaların yoxlanması
            int subCount = await _context.ProductCategories.CountAsync(c => c.ParentCategoryId == id);
            if (subCount > 0)
            {
                TempData["Warning"] = $"'{category.Name}' kateqoriyasına bağlı {subCount} ədəd alt kateqoriya mövcuddur! Əvvəlcə alt kateqoriyaları silməli və ya onların ana kateqoriyasını dəyişməlisiniz.";
                return RedirectToAction(nameof(Index));
            }

            _context.ProductCategories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"'{category.Name}' kateqoriyası uğurla silindi!";
            return RedirectToAction(nameof(Index));
        }

        #region Helpers

        private async Task PopulateParentCategoriesAsync(int? selectedParentId = null, int? currentCategoryId = null)
        {
            var query = _context.ProductCategories
                .Where(c => c.ParentCategoryId == null)
                .AsQueryable();

            if (currentCategoryId.HasValue)
            {
                query = query.Where(c => c.Id != currentCategoryId.Value);
            }

            var parents = await query.OrderBy(c => c.Name).ToListAsync();
            ViewBag.ParentCategories = new SelectList(parents, "Id", "Name", selectedParentId);
        }

        private static string GenerateSlug(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            var slug = input.Trim().ToLowerInvariant();

            // Azərbaycan hərflərini əvəzlə
            slug = slug.Replace('ə', 'e')
                       .Replace('ı', 'i')
                       .Replace('ö', 'o')
                       .Replace('ü', 'u')
                       .Replace('ç', 'c')
                       .Replace('ş', 's')
                       .Replace('ğ', 'g');

            // Qeyri-hərf-rəqəm simvolları defis ilə əvəzlə
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-");
            slug = Regex.Replace(slug, @"-+", "-");

            return slug.Trim('-');
        }

        #endregion
    }
}
