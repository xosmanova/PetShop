using System.Text.RegularExpressions;
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
    public class BlogController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public BlogController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Admin/Blog
        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            const int pageSize = 10;
            if (page < 1) page = 1;

            var query = _context.BlogPosts
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.Trim().ToLower();
                query = query.Where(b =>
                    b.Title.ToLower().Contains(lowerSearch) ||
                    b.AuthorName.ToLower().Contains(lowerSearch) ||
                    b.Summary.ToLower().Contains(lowerSearch));
            }

            var totalCount = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            if (page > totalPages) page = totalPages;

            var posts = await query
                .OrderByDescending(b => b.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new AdminBlogListViewModel
            {
                Posts = posts,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                Search = search
            };

            return View(viewModel);
        }

        // GET: Admin/Blog/Create
        public IActionResult Create()
        {
            return View(new BlogPostFormViewModel());
        }

        // POST: Admin/Blog/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BlogPostFormViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Slug))
            {
                model.Slug = GenerateSlug(model.Title);
            }
            else
            {
                model.Slug = GenerateSlug(model.Slug);
            }

            // Slug unikal olmalıdır, təkrarlanarsa avtomatik suffix əlavə et
            var baseSlug = model.Slug;
            int counter = 1;
            while (await _context.BlogPosts.AnyAsync(b => b.Slug == model.Slug))
            {
                model.Slug = $"{baseSlug}-{counter++}";
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string? imageUrl = null;
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                imageUrl = await SaveImageFileAsync(model.ImageFile);
            }

            var post = new BlogPost
            {
                Title = model.Title.Trim(),
                Slug = model.Slug,
                Summary = model.Summary.Trim(),
                Content = model.Content,
                AuthorName = model.AuthorName.Trim(),
                ImageUrl = imageUrl,
                CreatedDate = DateTime.UtcNow
            };

            _context.BlogPosts.Add(post);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"'{post.Title}' məqaləsi uğurla əlavə edildi!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Blog/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var post = await _context.BlogPosts.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            var model = new BlogPostFormViewModel
            {
                Id = post.Id,
                Title = post.Title,
                Slug = post.Slug,
                Summary = post.Summary,
                Content = post.Content,
                AuthorName = post.AuthorName,
                ExistingImageUrl = post.ImageUrl
            };

            return View(model);
        }

        // POST: Admin/Blog/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BlogPostFormViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var post = await _context.BlogPosts.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(model.Slug))
            {
                model.Slug = GenerateSlug(model.Title);
            }
            else
            {
                model.Slug = GenerateSlug(model.Slug);
            }

            // Slug-un başqa məqalədə istifadə olunmadığını yoxla
            var baseSlug = model.Slug;
            int counter = 1;
            while (await _context.BlogPosts.AnyAsync(b => b.Slug == model.Slug && b.Id != id))
            {
                model.Slug = $"{baseSlug}-{counter++}";
            }

            if (!ModelState.IsValid)
            {
                model.ExistingImageUrl = post.ImageUrl;
                return View(model);
            }

            // Yeni şəkil yüklənibsə
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var newImageUrl = await SaveImageFileAsync(model.ImageFile);
                if (!string.IsNullOrEmpty(newImageUrl))
                {
                    DeleteImageFileFromDisk(post.ImageUrl);
                    post.ImageUrl = newImageUrl;
                }
            }

            post.Title = model.Title.Trim();
            post.Slug = model.Slug;
            post.Summary = model.Summary.Trim();
            post.Content = model.Content;
            post.AuthorName = model.AuthorName.Trim();

            await _context.SaveChangesAsync();

            TempData["Success"] = $"'{post.Title}' məqaləsi uğurla yeniləndi!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Blog/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var post = await _context.BlogPosts.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            DeleteImageFileFromDisk(post.ImageUrl);

            _context.BlogPosts.Remove(post);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"'{post.Title}' məqaləsi uğurla silindi!";
            return RedirectToAction(nameof(Index));
        }

        #region Helpers

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

        private async Task<string?> SaveImageFileAsync(IFormFile file)
        {
            try
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "blog");
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

                return $"/images/blog/{uniqueFileName}";
            }
            catch
            {
                return null;
            }
        }

        private void DeleteImageFileFromDisk(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;

            // Yalnız admin upload qovluğundakı faylları sil (şablon şəkillərini qorumaq üçün)
            if (imageUrl.StartsWith("/images/blog/"))
            {
                try
                {
                    var relativePath = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                    var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }
                catch { }
            }
        }

        #endregion
    }
}
