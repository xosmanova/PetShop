using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PetShop.DataContext.Entites;

namespace PetShop.DataContext
{
    public class AppDbContext : DbContext
    {

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<AnimalType> AnimalTypes { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductAnimalType> ProductAnimalTypes { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Basket> Baskets { get; set; }
        public DbSet<BasketItem> BasketItems { get; set; }
        public DbSet<Wishlist> Wishlist { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ProductAnimalType - composite key (Many-to-Many join entity)
            modelBuilder.Entity<ProductAnimalType>()
                .HasKey(pa => new { pa.ProductId, pa.AnimalTypeId });

            modelBuilder.Entity<ProductAnimalType>()
                .HasOne(pa => pa.Product)
                .WithMany(p => p.ProductAnimalTypes)
                .HasForeignKey(pa => pa.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductAnimalType>()
                .HasOne(pa => pa.AnimalType)
                .WithMany(a => a.ProductAnimalTypes)
                .HasForeignKey(pa => pa.AnimalTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            // ProductCategory - özünə istinad (Parent/Sub)
            modelBuilder.Entity<ProductCategory>()
                .HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductCategory>()
                .HasIndex(c => c.Slug)
                .IsUnique();
            modelBuilder.Entity<Basket>()
                .HasMany(b => b.BasketItems)
                .WithOne(bi => bi.Basket)
                .HasForeignKey(bi => bi.BasketId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BasketItem>()
                .HasOne(bi => bi.Product)
                .WithMany(p => p.BasketItems)
                .HasForeignKey(bi => bi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BasketItem>()
                .HasOne(bi => bi.ProductVariant)
                .WithMany()
                .HasForeignKey(bi => bi.ProductVariantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product - ProductCategory (One-to-Many)
            modelBuilder.Entity<Product>()
                .HasOne(p => p.ProductCategory)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.ProductCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product - Brand (One-to-Many, optional)
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Brand)
                .WithMany(b => b.Products)
                .HasForeignKey(p => p.BrandId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product - ProductVariant (One-to-Many)
            modelBuilder.Entity<ProductVariant>()
                .HasOne(v => v.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Product - ProductImage (One-to-Many)
            modelBuilder.Entity<ProductImage>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Wishlist>()
                .HasOne(w => w.Product)
                .WithMany()
                .HasForeignKey(w => w.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Wishlist>()
                .HasIndex(w => new { w.SessionId, w.ProductId })
                .IsUnique();

            // Decimal precision
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Product>()
                .Property(p => p.DiscountPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<ProductVariant>()
                .Property(v => v.PriceModifier)
                .HasColumnType("decimal(18,2)");

            // ================= SEED DATA =================

            // 1) AnimalType
            modelBuilder.Entity<AnimalType>().HasData(
                new AnimalType { Id = 1, Name = "İt" },
                new AnimalType { Id = 2, Name = "Pişik" },
                new AnimalType { Id = 3, Name = "Quş" },
                new AnimalType { Id = 4, Name = "Balıq" }
            );

            // 2) ProductCategory (Parent + Sub-category)
            modelBuilder.Entity<ProductCategory>().HasData(
            new ProductCategory { Id = 1, Name = "Yem", Slug = "food", ParentCategoryId = null, IconClass = "ph:bowl-food" },
            new ProductCategory { Id = 2, Name = "Geyim", Slug = "cloth", ParentCategoryId = null, IconClass = "ph:t-shirt" },
            new ProductCategory { Id = 3, Name = "İt Yemi", Slug = "food-dog", ParentCategoryId = 1, IconClass = "ph:dog" },
            new ProductCategory { Id = 4, Name = "Pişik Yemi", Slug = "food-cat", ParentCategoryId = 1, IconClass = "ph:cat" },
            new ProductCategory { Id = 5, Name = "Quş Yemi", Slug = "food-bird", ParentCategoryId = 1, IconClass = "ph:bird" },
            new ProductCategory { Id = 6, Name = "İt Geyimi", Slug = "cloth-dog", ParentCategoryId = 2, IconClass = "ph:dog" },
            new ProductCategory { Id = 7, Name = "Pişik Geyimi", Slug = "cloth-cat", ParentCategoryId = 2, IconClass = "ph:cat" }
            );

            // 3) Brand
            modelBuilder.Entity<Brand>().HasData(
                new Brand { Id = 1, Name = "Royal Canin" },
                new Brand { Id = 2, Name = "Whiskas" },
                new Brand { Id = 3, Name = "Pedigree" },
                new Brand { Id = 4, Name = "Denim" },
                new Brand { Id = 5, Name = "Puma" },
                new Brand { Id = 6, Name = "Klaws" }
            );

            // 4) Product
            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 1,
                    Name = "Royal Canin Dog Food 3kg",
                    Description = "Böyük yaşlı itlər üçün premium yem",
                    Price = 45.99m,
                    StockQuantity = 100,
                    Sku = "RC-DOG-3KG",
                    IsActive = true,
                    CreatedAt = new DateTime(2025, 1, 1),
                    ProductCategoryId = 3,
                    BrandId = 1
                },
                new Product
                {
                    Id = 2,
                    Name = "Whiskas Cat Food 1.5kg",
                    Description = "Yetişkin pişiklər üçün balıqlı yem",
                    Price = 18.50m,
                    StockQuantity = 150,
                    Sku = "WHI-CAT-1.5KG",
                    IsActive = true,
                    CreatedAt = new DateTime(2025, 1, 1),
                    ProductCategoryId = 4,
                    BrandId = 2
                },
                new Product
                {
                    Id = 3,
                    Name = "Pedigree Puppy Food 2kg",
                    Description = "Kiçik cinslər üçün balaca it yemi",
                    Price = 22.00m,
                    StockQuantity = 80,
                    Sku = "PED-PUP-2KG",
                    IsActive = true,
                    CreatedAt = new DateTime(2025, 1, 1),
                    ProductCategoryId = 3,
                    BrandId = 3
                },
                new Product
                {
                    Id = 4,
                    Name = "Dog Winter Jacket M",
                    Description = "Orta ölçülü itlər üçün qış ceketi",
                    Price = 15.99m,
                    StockQuantity = 40,
                    Sku = "CLT-DOG-M",
                    IsActive = true,
                    CreatedAt = new DateTime(2025, 1, 1),
                    ProductCategoryId = 6,
                    BrandId = 4
                }
            );

            // 5) ProductAnimalType (composite key join)
            modelBuilder.Entity<ProductAnimalType>().HasData(
                new ProductAnimalType { ProductId = 1, AnimalTypeId = 1 },
                new ProductAnimalType { ProductId = 2, AnimalTypeId = 2 },
                new ProductAnimalType { ProductId = 3, AnimalTypeId = 1 },
                new ProductAnimalType { ProductId = 4, AnimalTypeId = 1 }
            );

            // 6) ProductVariant
            modelBuilder.Entity<ProductVariant>().HasData(
                new ProductVariant { Id = 1, ProductId = 1, Weight = "3kg", PriceModifier = 0, StockQuantity = 100 },
                new ProductVariant { Id = 2, ProductId = 2, Weight = "1.5kg", PriceModifier = 0, StockQuantity = 150 },
                new ProductVariant { Id = 3, ProductId = 4, Size = "M", Color = "Blue", PriceModifier = 0, StockQuantity = 40 },
                new ProductVariant { Id = 4, ProductId = 4, Size = "L", Color = "Blue", PriceModifier = 3.00m, StockQuantity = 20 }
            );

            // 7) ProductImage
            modelBuilder.Entity<ProductImage>().HasData(
                new ProductImage { Id = 1, ProductId = 1, ImageUrl = "/images/products/royal-canin-dog.jpg", IsMain = true },
                new ProductImage { Id = 2, ProductId = 2, ImageUrl = "/images/products/whiskas-cat.jpg", IsMain = true },
                new ProductImage { Id = 3, ProductId = 3, ImageUrl = "/images/products/pedigree-puppy.jpg", IsMain = true },
                new ProductImage { Id = 4, ProductId = 4, ImageUrl = "/images/products/dog-jacket.jpg", IsMain = true }
            );
        }
    }
}
