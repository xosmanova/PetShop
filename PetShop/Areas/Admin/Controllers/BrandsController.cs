using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.Areas.Admin.Models;
using PetShop.DataContext;
using PetShop.DataContext.Entites;

namespace PetShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BrandsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public BrandsController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Admin/Brands
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Brands
                .Include(b => b.Products)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.Trim().ToLower();
                query = query.Where(b => b.Name.ToLower().Contains(lowerSearch));
            }

            var brands = await query.OrderBy(b => b.Name).ToListAsync();
            ViewBag.Search = search;
            return View(brands);
        }

        // GET: Admin/Brands/Create
        public IActionResult Create()
        {
            return View(new BrandFormViewModel());
        }

        // POST: Admin/Brands/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BrandFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var brand = new Brand
            {
                Name = model.Name.Trim()
            };

            if (model.LogoFile != null && model.LogoFile.Length > 0)
            {
                var fileName = await SaveLogoFileAsync(model.LogoFile);
                if (!string.IsNullOrEmpty(fileName))
                {
                    brand.LogoUrl = "/images/brands/" + fileName;
                }
            }

            _context.Brands.Add(brand);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"'{brand.Name}' brendi uğurla əlavə edildi!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Brands/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
            {
                TempData["Error"] = "Brend tapılmadı!";
                return RedirectToAction(nameof(Index));
            }

            var model = new BrandFormViewModel
            {
                Id = brand.Id,
                Name = brand.Name,
                ExistingLogoUrl = brand.LogoUrl
            };

            return View(model);
        }

        // POST: Admin/Brands/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BrandFormViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
            {
                TempData["Error"] = "Redaktə ediləcək brend tapılmadı!";
                return RedirectToAction(nameof(Index));
            }

            brand.Name = model.Name.Trim();

            if (model.LogoFile != null && model.LogoFile.Length > 0)
            {
                var fileName = await SaveLogoFileAsync(model.LogoFile);
                if (!string.IsNullOrEmpty(fileName))
                {
                    DeleteLogoFromDisk(brand.LogoUrl);
                    brand.LogoUrl = "/images/brands/" + fileName;
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"'{brand.Name}' brendi uğurla yeniləndi!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Brands/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var brand = await _context.Brands
                .Include(b => b.Products)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (brand == null)
            {
                TempData["Error"] = "Silinəcək brend tapılmadı!";
                return RedirectToAction(nameof(Index));
            }

            // Əgər həmin brendə bağlı Product varsa, xəbərdarlıq göstər və silməyə icazə vermə
            int productCount = await _context.Products.CountAsync(p => p.BrandId == id);
            if (productCount > 0)
            {
                TempData["Warning"] = $"'{brand.Name}' brendinə bağlı {productCount} ədəd məhsul mövcuddur! Məhsulu olan brendi silmək mümkün deyil. Əvvəlcə həmin məhsulların brendini dəyişməli və ya onları silməlisiniz.";
                return RedirectToAction(nameof(Index));
            }

            DeleteLogoFromDisk(brand.LogoUrl);

            _context.Brands.Remove(brand);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"'{brand.Name}' brendi uğurla silindi!";
            return RedirectToAction(nameof(Index));
        }

        #region Helpers

        private async Task<string?> SaveLogoFileAsync(IFormFile file)
        {
            try
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "brands");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var extension = Path.GetExtension(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return uniqueFileName;
            }
            catch
            {
                return null;
            }
        }

        private void DeleteLogoFromDisk(string? logoUrl)
        {
            if (string.IsNullOrEmpty(logoUrl)) return;

            if (logoUrl.StartsWith("/images/brands/"))
            {
                var relativePath = logoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);
                if (System.IO.File.Exists(fullPath))
                {
                    try
                    {
                        System.IO.File.Delete(fullPath);
                    }
                    catch
                    {
                    }
                }
            }
        }

        #endregion
    }
}
