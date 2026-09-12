using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PetShop.DataContext.Entites;

namespace PetShop.DataContext
{
    public static class DbInitializer
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // 1. Rolların yaradılması
            string[] roleNames = { "Admin", "Member" };
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. Admin istifadəçisinin yaradılması
            const string adminEmail = "admin@petshop.com";
            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);

            if (existingAdmin == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Admin",
                    LastName = "Hasan",
                    PhoneNumber = "+994501234567",
                    EmailConfirmed = true
                };

                var createAdmin = await userManager.CreateAsync(adminUser, "Admin123!");
                if (createAdmin.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
            else
            {
                // Mövcuddursa, Admin rolunun olduğunu təmin et
                if (!await userManager.IsInRoleAsync(existingAdmin, "Admin"))
                {
                    await userManager.AddToRoleAsync(existingAdmin, "Admin");
                }
            }

            // 3. Demo kuponların yaradılması
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var pet10Coupon = context.Coupons.FirstOrDefault(c => c.Code == "PET10");
            if (pet10Coupon != null)
            {
                if (pet10Coupon.MinOrderAmount > 10)
                {
                    pet10Coupon.MinOrderAmount = 10;
                    await context.SaveChangesAsync();
                }
            }
            else if (!context.Coupons.Any())
            {
                context.Coupons.AddRange(
                    new Coupon
                    {
                        Code = "PET10",
                        DiscountType = DiscountType.Percentage,
                        DiscountValue = 10,
                        ExpiryDate = DateTime.UtcNow.AddMonths(3),
                        MinOrderAmount = 10,
                        IsActive = true
                    },
                    new Coupon
                    {
                        Code = "SAVE20",
                        DiscountType = DiscountType.Fixed,
                        DiscountValue = 20,
                        ExpiryDate = DateTime.UtcNow.AddMonths(6),
                        MinOrderAmount = 50,
                        IsActive = true
                    }
                );
                await context.SaveChangesAsync();
            }

            // 4. Demo endirimli məhsulların təmin edilməsi (Offers səhifəsi üçün)
            var p3 = context.Products.FirstOrDefault(p => p.Id == 3);
            if (p3 != null && (!p3.DiscountPrice.HasValue || p3.DiscountPrice.Value >= p3.Price))
            {
                p3.Price = 20.00m;
                p3.DiscountPrice = 15.00m;
            }

            var p2 = context.Products.FirstOrDefault(p => p.Id == 2);
            if (p2 != null && (!p2.DiscountPrice.HasValue || p2.DiscountPrice.Value >= p2.Price))
            {
                p2.DiscountPrice = 14.50m;
            }

            // 5. Demo geyim məhsulu və ölçü (Size) variantları: S (12), M (3), L (0), XL (25)
            var clothCat = context.ProductCategories.FirstOrDefault(c => c.Slug == "cloth-dog" || c.Name == "İt Geyimi");
            if (clothCat == null)
            {
                var parentCloth = context.ProductCategories.FirstOrDefault(c => c.Slug == "cloth" || c.Name == "Geyim");
                clothCat = new ProductCategory
                {
                    Name = "İt Geyimi",
                    Slug = "cloth-dog",
                    ParentCategoryId = parentCloth?.Id
                };
                context.ProductCategories.Add(clothCat);
                await context.SaveChangesAsync();
            }

            var catCatCloth = context.ProductCategories.FirstOrDefault(c => c.Name == "Pişik Geyimi");
            var parentClothCat = context.ProductCategories.FirstOrDefault(c => c.Slug == "cloth" || c.Name == "Geyim");
            if (catCatCloth != null && catCatCloth.ParentCategoryId == null && parentClothCat != null)
            {
                catCatCloth.ParentCategoryId = parentClothCat.Id;
                await context.SaveChangesAsync();
            }

            var clothingProduct = context.Products.Include(p => p.Variants).FirstOrDefault(p => p.ProductCategoryId == clothCat.Id);
            if (clothingProduct == null)
            {
                clothingProduct = new Product
                {
                    Name = "Qış İt Gödəkçəsi (Dog Jacket)",
                    Description = "İtlər üçün suya və küləyə davamlı, isti və rahat qış gödəkçəsi. Fərqli bədən ölçülərinə tam uyğundur.",
                    Price = 35.00m,
                    DiscountPrice = 29.90m,
                    StockQuantity = 40,
                    Sku = "DOG-JKT-01",
                    IsActive = true,
                    ProductCategoryId = clothCat.Id,
                    CreatedAt = DateTime.UtcNow,
                    Images = new List<ProductImage>
                    {
                        new ProductImage { ImageUrl = "/images/products/dog-jacket.jpg", IsMain = true }
                    }
                };
                context.Products.Add(clothingProduct);
                await context.SaveChangesAsync();
            }

            if (!context.ProductVariants.Any(v => v.ProductId == clothingProduct.Id))
            {
                context.ProductVariants.AddRange(
                    new ProductVariant { ProductId = clothingProduct.Id, Size = "S", Weight = null, Color = "Qırmızı", PriceModifier = 0m, StockQuantity = 12 },
                    new ProductVariant { ProductId = clothingProduct.Id, Size = "M", Weight = null, Color = "Qırmızı", PriceModifier = 2.00m, StockQuantity = 3 },
                    new ProductVariant { ProductId = clothingProduct.Id, Size = "L", Weight = null, Color = "Qırmızı", PriceModifier = 4.00m, StockQuantity = 0 },
                    new ProductVariant { ProductId = clothingProduct.Id, Size = "XL", Weight = null, Color = "Qırmızı", PriceModifier = 6.00m, StockQuantity = 25 }
                );
                await context.SaveChangesAsync();
            }

            // 6. Yem məhsulu üçün 500g (Stok: 48 ədəd) variantının təmin edilməsi
            var foodProduct = context.Products.Include(p => p.Variants).FirstOrDefault(p => p.Id == 1);
            if (foodProduct != null && !foodProduct.Variants.Any(v => v.Weight == "500g"))
            {
                context.ProductVariants.Add(new ProductVariant
                {
                    ProductId = foodProduct.Id,
                    Weight = "500g",
                    Size = null,
                    PriceModifier = -10.00m,
                    StockQuantity = 48
                });
                await context.SaveChangesAsync();
            }

            await context.SaveChangesAsync();
        }
    }
}
