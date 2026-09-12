using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using PetShop.DataContext;
using PetShop.DataContext.Entites;
using PetShop.Services;
using Stripe;

namespace PetShop
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Stripe konfiqurasiyası
            StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

            // Add services to the container.
            builder.Services.AddControllersWithViews(options =>
            {
                // Bütün POST, PUT, DELETE metodları üçün avtomatik AntiForgeryToken təsdiqi (CSRF müdafiəsi)
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            });

            // Antiforgery Header dəstəyi (AJAX üçün)
            builder.Services.AddAntiforgery(options =>
            {
                options.HeaderName = "RequestVerificationToken";
            });

            // HTML Encoder konfiqurasiyası (Azərbaycan və xüsusi simvolların HTML entity-yə çevrilməməsi üçün)
            builder.Services.AddSingleton(System.Text.Encodings.Web.HtmlEncoder.Create(System.Text.Unicode.UnicodeRanges.All));

            // HSTS konfiqurasiyası (Production üçün)
            builder.Services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });

            // Rate Limiting (Brute-force və DoS hücumlarından müdafiə)
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // Login üçün: 1 dəqiqədə maksimum 5 cəhd
                options.AddFixedWindowLimiter("login_policy", opt =>
                {
                    opt.PermitLimit = 5;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 0;
                });

                // Checkout üçün: 1 dəqiqədə maksimum 10 sorğu
                options.AddFixedWindowLimiter("checkout_policy", opt =>
                {
                    opt.PermitLimit = 10;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 0;
                });

                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.HttpContext.Response.ContentType = "text/html; charset=utf-8";
                    await context.HttpContext.Response.WriteAsync(
                        "<!DOCTYPE html><html lang='az'><head><meta charset='utf-8'><title>429 Çox Sayda Sorğu</title></head>" +
                        "<body style='font-family:Segoe UI,sans-serif;text-align:center;padding:60px 20px;background:#f8f9fa;color:#333;'>" +
                        "<div style='max-width:550px;margin:0 auto;background:#fff;padding:40px;border-radius:12px;box-shadow:0 4px 15px rgba(0,0,0,0.08);'>" +
                        "<h1 style='color:#dc3545;font-size:26px;margin-bottom:15px;'>⚠️ Çox sayda sorğu aşkarlandı</h1>" +
                        "<p style='color:#6c757d;font-size:15px;line-height:1.6;'>Təhlükəsizlik məqsədilə qısa müddətdə göndərilən sorğu limitiniz dolmuşdur. Zəhmət olmasa bir qədər sonra yenidən cəhd edin.</p>" +
                        "<a href='/' style='display:inline-block;margin-top:20px;padding:10px 24px;background:#198754;color:#fff;text-decoration:none;border-radius:6px;font-weight:600;'>Ana Səhifəyə Qayıt</a>" +
                        "</div></body></html>", cancellationToken: token);
                };
            });

            // MemoryCache (Sürətli yaddaş keşlənməsi)
            builder.Services.AddMemoryCache();

            // HTTP Response Compression (Brotli və Gzip)
            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { "image/svg+xml", "application/json", "text/css", "application/javascript", "text/html", "text/plain" });
            });

            builder.Services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = System.IO.Compression.CompressionLevel.Fastest;
            });

            builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
            {
                options.Level = System.IO.Compression.CompressionLevel.Fastest;
            });

            // Email xidməti
            builder.Services.AddScoped<IEmailService, EmailService>();

            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("Default"),
                    sqlOptions => sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
            });

            // ASP.NET Core Identity konfiqurasiyası
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.SlidingExpiration = true;
            });

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromDays(7);
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.HttpOnly = true;
                options.Cookie.MaxAge = TimeSpan.FromDays(7);
            });

            var app = builder.Build();

            // Seed Roles and default Admin User
            await DbInitializer.SeedRolesAndAdminAsync(app.Services);

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
                app.UseHttpsRedirection();
            }

            // HTTP Response Compression middleware
            app.UseResponseCompression();

            app.UseStaticFiles();

            app.UseRouting();
            app.UseRateLimiter();
            app.UseSession();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "product_detail",
                pattern: "product/{id:int}",
                defaults: new { controller = "Shop", action = "Details" });

            app.MapControllerRoute(
                name: "blog_detail",
                pattern: "blog/{slug}",
                defaults: new { controller = "Blog", action = "Details" });

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            await app.RunAsync();
        }
    }
}
