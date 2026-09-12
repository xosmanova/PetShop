using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PetShop.Areas.Admin.Models;
using PetShop.DataContext;
using PetShop.DataContext.Entites;

namespace PetShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductsController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Admin/Products
        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            const int pageSize = 10;
            if (page < 1) page = 1;

            var query = _context.Products
                .AsNoTracking()
                .AsSplitQuery()
                .Include(p => p.ProductCategory)
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.Trim().ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(lowerSearch) ||
                    (p.Sku != null && p.Sku.ToLower().Contains(lowerSearch)) ||
                    p.ProductCategory.Name.ToLower().Contains(lowerSearch) ||
                    (p.Brand != null && p.Brand.Name.ToLower().Contains(lowerSearch)));
            }

            var totalCount = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            if (page > totalPages) page = totalPages;

            var products = await query
                .OrderByDescending(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new ProductListViewModel
            {
                Products = products,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalCount = totalCount,
                SearchQuery = search
            };

            return View(viewModel);
        }

        // GET: Admin/Products/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropdownsAsync();
            return View(new ProductFormViewModel());
        }

        // POST: Admin/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model.ProductCategoryId, model.BrandId);
                return View(model);
            }

            var product = new Product
            {
                Name = model.Name.Trim(),
                Description = model.Description?.Trim(),
                Price = model.Price,
                DiscountPrice = model.DiscountPrice,
                StockQuantity = model.StockQuantity,
                Sku = model.Sku?.Trim(),
                IsActive = model.IsActive,
                ProductCategoryId = model.ProductCategoryId,
                BrandId = model.BrandId,
                CreatedAt = DateTime.UtcNow
            };

            // Şəkil yüklənməsi (wwwroot/images/products)
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var uniqueFileName = await SaveImageFileAsync(model.ImageFile);
                if (!string.IsNullOrEmpty(uniqueFileName))
                {
                    product.Images.Add(new ProductImage
                    {
                        ImageUrl = "/images/products/" + uniqueFileName,
                        IsMain = true
                    });
                }
            }

            // Variantların əlavə edilməsi (Yem üçün Weight, Geyim üçün Size)
            var category = await _context.ProductCategories.FindAsync(model.ProductCategoryId);
            bool isClothing = category != null && (category.Slug.StartsWith("cloth") || category.Name.Contains("Geyim"));

            if (model.Variants != null && model.Variants.Count > 0)
            {
                foreach (var v in model.Variants)
                {
                    if (v.IsDeleted) continue;

                    string? cleanSize = string.IsNullOrWhiteSpace(v.Size) ? null : v.Size.Trim();
                    string? cleanWeight = string.IsNullOrWhiteSpace(v.Weight) ? null : v.Weight.Trim();

                    // Qarşılıqlı müstəsnalıq (Weight və ya Size)
                    if (isClothing)
                    {
                        cleanWeight = null;
                    }
                    else
                    {
                        cleanSize = null;
                    }

                    if (!string.IsNullOrWhiteSpace(cleanSize) || !string.IsNullOrWhiteSpace(cleanWeight) ||
                        !string.IsNullOrWhiteSpace(v.Color) || v.PriceModifier != 0 || v.StockQuantity > 0)
                    {
                        product.Variants.Add(new ProductVariant
                        {
                            Size = cleanSize,
                            Color = string.IsNullOrWhiteSpace(v.Color) ? null : v.Color.Trim(),
                            Weight = cleanWeight,
                            PriceModifier = v.PriceModifier,
                            StockQuantity = v.StockQuantity
                        });
                    }
                }
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"'{product.Name}' məhsulu uğurla əlavə edildi!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Products/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                TempData["Error"] = "Məhsul tapılmadı!";
                return RedirectToAction(nameof(Index));
            }

            var mainImage = product.Images.FirstOrDefault(i => i.IsMain) ?? product.Images.FirstOrDefault();

            var model = new ProductFormViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                DiscountPrice = product.DiscountPrice,
                StockQuantity = product.StockQuantity,
                Sku = product.Sku,
                IsActive = product.IsActive,
                ProductCategoryId = product.ProductCategoryId,
                BrandId = product.BrandId,
                ExistingImageUrl = mainImage?.ImageUrl,
                Variants = product.Variants.Select(v => new ProductVariantInputModel
                {
                    Id = v.Id,
                    Size = v.Size,
                    Color = v.Color,
                    Weight = v.Weight,
                    PriceModifier = v.PriceModifier,
                    StockQuantity = v.StockQuantity,
                    IsDeleted = false
                }).ToList()
            };

            await PopulateDropdownsAsync(product.ProductCategoryId, product.BrandId);
            return View(model);
        }

        // POST: Admin/Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductFormViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model.ProductCategoryId, model.BrandId);
                return View(model);
            }

            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                TempData["Error"] = "Redaktə ediləcək məhsul tapılmadı!";
                return RedirectToAction(nameof(Index));
            }

            // Əsas sahələri yenilə
            product.Name = model.Name.Trim();
            product.Description = model.Description?.Trim();
            product.Price = model.Price;
            product.DiscountPrice = model.DiscountPrice;
            product.StockQuantity = model.StockQuantity;
            product.Sku = model.Sku?.Trim();
            product.IsActive = model.IsActive;
            product.ProductCategoryId = model.ProductCategoryId;
            product.BrandId = model.BrandId;

            // Şəkil yenilənməsi
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var uniqueFileName = await SaveImageFileAsync(model.ImageFile);
                if (!string.IsNullOrEmpty(uniqueFileName))
                {
                    var mainImage = product.Images.FirstOrDefault(i => i.IsMain);
                    if (mainImage != null)
                    {
                        // Köhnə şəkili diskdən sil
                        DeleteImageFileFromDisk(mainImage.ImageUrl);
                        mainImage.ImageUrl = "/images/products/" + uniqueFileName;
                    }
                    else
                    {
                        product.Images.Add(new ProductImage
                        {
                            ImageUrl = "/images/products/" + uniqueFileName,
                            IsMain = true,
                            ProductId = product.Id
                        });
                    }
                }
            }

            // Mövcud variant-ları edit/delete/add et
            var submittedVariants = model.Variants ?? new List<ProductVariantInputModel>();

            // 1. Silinəcək variantlar (IsDeleted = true və ya ümumiyyətlə siyahıdan çıxarılmış)
            var submittedIds = submittedVariants.Where(v => !v.IsDeleted && v.Id > 0).Select(v => v.Id).ToHashSet();
            var variantsToRemove = product.Variants.Where(v => !submittedIds.Contains(v.Id)).ToList();
            foreach (var toRemove in variantsToRemove)
            {
                _context.ProductVariants.Remove(toRemove);
            }

            // 2. Mövcud variantların redaktəsi və yenilərinin əlavə edilməsi (Yem üçün Weight, Geyim üçün Size)
            var category = await _context.ProductCategories.FindAsync(model.ProductCategoryId);
            bool isClothing = category != null && (category.Slug.StartsWith("cloth") || category.Name.Contains("Geyim"));

            foreach (var v in submittedVariants)
            {
                if (v.IsDeleted) continue;

                string? cleanSize = string.IsNullOrWhiteSpace(v.Size) ? null : v.Size.Trim();
                string? cleanWeight = string.IsNullOrWhiteSpace(v.Weight) ? null : v.Weight.Trim();

                if (isClothing)
                {
                    cleanWeight = null;
                }
                else
                {
                    cleanSize = null;
                }

                if (v.Id > 0)
                {
                    var existingVariant = product.Variants.FirstOrDefault(x => x.Id == v.Id);
                    if (existingVariant != null)
                    {
                        existingVariant.Size = cleanSize;
                        existingVariant.Color = string.IsNullOrWhiteSpace(v.Color) ? null : v.Color.Trim();
                        existingVariant.Weight = cleanWeight;
                        existingVariant.PriceModifier = v.PriceModifier;
                        existingVariant.StockQuantity = v.StockQuantity;
                    }
                }
                else
                {
                    // Yeni variant (Id == 0)
                    if (!string.IsNullOrWhiteSpace(cleanSize) || !string.IsNullOrWhiteSpace(cleanWeight) ||
                        !string.IsNullOrWhiteSpace(v.Color) || v.PriceModifier != 0 || v.StockQuantity > 0)
                    {
                        product.Variants.Add(new ProductVariant
                        {
                            ProductId = product.Id,
                            Size = cleanSize,
                            Color = string.IsNullOrWhiteSpace(v.Color) ? null : v.Color.Trim(),
                            Weight = cleanWeight,
                            PriceModifier = v.PriceModifier,
                            StockQuantity = v.StockQuantity
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"'{product.Name}' məhsulu uğurla yeniləndi!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Products/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                TempData["Error"] = "Silinəcək məhsul tapılmadı!";
                return RedirectToAction(nameof(Index));
            }

            // Əgər həmin Product hər hansı Order/Səbətdə istifadə olunubsa, silməyə icazə vermə, soft delete et
            bool isUsedInOrders = await _context.BasketItems.AnyAsync(bi => bi.ProductId == id) ||
                                  await _context.OrderItems.AnyAsync(oi => oi.ProductId == id);

            if (isUsedInOrders)
            {
                product.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["Warning"] = $"'{product.Name}' məhsulu sifarişlərdə və ya aktiv səbətlərdə mövcud olduğu üçün tam silinmədi, statusu 'Qeyri-aktiv' (Soft Delete) edildi.";
                return RedirectToAction(nameof(Index));
            }

            // İstifadə olunmayıbsa: cascade sil
            // 1. Şəkilləri diskdən sil
            foreach (var image in product.Images)
            {
                DeleteImageFileFromDisk(image.ImageUrl);
            }

            // 2. Məhsulu bazadan sil (Images və Variants cascade konfiqurasiya edilib)
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"'{product.Name}' məhsulu və əlaqəli bütün məlumatlar uğurla silindi!";
            return RedirectToAction(nameof(Index));
        }

        #region Helper Methods

        private async Task PopulateDropdownsAsync(int? selectedCategoryId = null, int? selectedBrandId = null)
        {
            var categories = await _context.ProductCategories
                .OrderBy(c => c.Name)
                .ToListAsync();

            var brands = await _context.Brands
                .OrderBy(b => b.Name)
                .ToListAsync();

            var categoryTypeMap = categories.ToDictionary(
                c => c.Id.ToString(),
                c => (c.Slug.StartsWith("cloth") || c.Name.Contains("Geyim") ? "size" : "weight")
            );
            ViewBag.CategoryTypeMapJson = System.Text.Json.JsonSerializer.Serialize(categoryTypeMap);

            ViewBag.Categories = new SelectList(categories, "Id", "Name", selectedCategoryId);
            ViewBag.Brands = new SelectList(brands, "Id", "Name", selectedBrandId);
        }

        private async Task<string?> SaveImageFileAsync(IFormFile file)
        {
            try
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var extension = Path.GetExtension(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                return uniqueFileName;
            }
            catch
            {
                return null;
            }
        }

        private void DeleteImageFileFromDisk(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;

            // Yalnız məhsul upload qovluğundakı faylları sil (seed data fayllarını qorumaq üçün)
            if (imageUrl.StartsWith("/images/products/"))
            {
                var relativePath = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);
                if (System.IO.File.Exists(fullPath))
                {
                    try
                    {
                        System.IO.File.Delete(fullPath);
                    }
                    catch
                    {
                        // Fayl istifadədədirsə və ya icazə yoxdursa səssiz keç
                    }
                }
            }
        }

        #endregion
    }
}
