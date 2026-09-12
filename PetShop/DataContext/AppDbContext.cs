using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PetShop.DataContext.Entites;

namespace PetShop.DataContext
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
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
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<BlogPost> BlogPosts { get; set; }
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

            modelBuilder.Entity<Basket>()
                .HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Wishlist>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Coupon configuration
            modelBuilder.Entity<Coupon>()
                .HasIndex(c => c.Code)
                .IsUnique();

            modelBuilder.Entity<Coupon>()
                .Property(c => c.DiscountValue)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Coupon>()
                .Property(c => c.MinOrderAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.DiscountAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalPrice)
                .HasPrecision(18, 2);

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

            // Order - OrderItem (One-to-Many)
            modelBuilder.Entity<Order>()
                .HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderNumber)
                .IsUnique();

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.ProductVariant)
                .WithMany()
                .HasForeignKey(oi => oi.ProductVariantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.UnitPrice)
                .HasColumnType("decimal(18,2)");

            // Review configuration
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasIndex(r => new { r.ProductId, r.UserId })
                .IsUnique();

            // BlogPost configuration
            modelBuilder.Entity<BlogPost>()
                .HasIndex(b => b.Slug)
                .IsUnique();

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

            // 8) BlogPost
            modelBuilder.Entity<BlogPost>().HasData(
                new BlogPost
                {
                    Id = 1,
                    Title = "10 Reasons to be helpful towards any animals",
                    Slug = "10-reasons-to-be-helpful-towards-any-animals",
                    Summary = "At the core of our practice is the idea that cities are the incubators of our greatest achievements, and the best hope for a sustainable future.",
                    Content = "Heyvanlara qayğı göstərmək və onların ehtiyaclarını qarşılamaq hər bir cəmiyyətin məsuliyyətidir. Bu yazıda dörd pəncəli dostlarımıza necə dəstək ola biləcəyimizi və bu addımların ətraf mühitimizə necə müsbət təsir etdiyini araşdırırıq.\n\n1. Şəfqət və empatiya hissinin formalaşması: Heyvanlarla ünsiyyət insanlarda, xüsusilə uşaqlarda mərhəmət və məsuliyyət hissini inkişaf etdirir.\n2. Təbiət və biomüxtəlifliyin qorunması: Canlıların sağlam həyat mühitinə sahib olması ekosistemin tarazlığını təmin edir.\n3. Küçə heyvanlarına sığınacaq və yemək təminatı: Xüsusilə qış aylarında isti sığınacaq və quru yem onların həyatda qalması üçün həyati vacibdir.\n4. Baytarlıq və peyvəndlərin əhəmiyyəti: Müntəzəm peyvəndlər heyvanları təhlükəli infeksiyalardan qoruyur.\n5. Düzgün sahiblənmə mədəniyyəti: Heyvan almaq əvəzinə sığınacaqlardan sahiblənmək bir canı xilas etmək deməkdir.",
                    ImageUrl = "/images/blog1.jpg",
                    AuthorName = "Admin",
                    CreatedDate = new DateTime(2025, 2, 20, 10, 0, 0, DateTimeKind.Utc)
                },
                new BlogPost
                {
                    Id = 2,
                    Title = "How to know your pet is hungry",
                    Slug = "how-to-know-your-pet-is-hungry",
                    Summary = "Ev heyvanınızın ac olduğunu necə anlaya bilərsiniz? Qidalanma rejimi, aclıq siqnalları və düzgün porsiya seçimi haqqında əsas məsləhətlər.",
                    Content = "Ev heyvanları danışa bilməsə də, onların davranışları bizə çox şey deyir. Aclıq siqnalını düzgün anlamaq onların həddindən artıq kökəlməsinin və ya qidasız qalmasının qarşısını alır.\n\n- Qabının yanında gözləmək və ya qabı döyəcləmək: Pişik və itlərin ən yayğın vərdişlərindən biridir.\n- Narahatlıq və diqqət axtarışı: Əgər ev heyvanınız adi vaxtdan daha çox ayaqlarınıza sürtünür və ya səslər çıxarırsa, yemək vaxtı gəlmiş ola bilər.\n- Düzgün yemləmə qrafiki: Heyvanları gün ərzində eyni saatlarda yemləmək onların həzm sistemini tənzimləyir.\n- Porsiyanın çəkiyə uyğunluğu: Yem qablaşdırmasındakı cədvələ uyğun olaraq çəkiyə və yaşa uyğun qrammatura təyin edilməlidir. Təbii və quru yemlərin balansı mütləq qorunmalıdır.",
                    ImageUrl = "/images/blog2.jpg",
                    AuthorName = "Dr. Leyla",
                    CreatedDate = new DateTime(2025, 2, 21, 12, 30, 0, DateTimeKind.Utc)
                },
                new BlogPost
                {
                    Id = 3,
                    Title = "Best home for your pets",
                    Slug = "best-home-for-your-pets",
                    Summary = "Ev şəraitində heyvanlar üçün rahat, təhlükəsiz və əyləncəli mühitin yaradılması üzrə bələdçi.",
                    Content = "Evdə heyvan saxlamaq böyük sevinc bəxş etməklə yanaşı, onların təhlükəsizliyi üçün evdə xüsusi şərait qurmağı tələb edir.\n\n- Rahat yataq və istirahət guşəsi: Hər bir heyvanın özünə aid sakit, cərəyan olmayan bir guşəsi olmalıdır.\n- Təhlükəli əşyaların uzaqlaşdırılması: Açıqda qalan elektrik naqilləri, zəhərli otaq bitkiləri və kiçik oyuncaqlar yığışdırılmalıdır.\n- Əyləncə və fiziki aktivlik: Pişiklər üçün cırmaqlama taxtası, itlər üçün diş qaşıyan oyuncaqlar onların enerjisini düzgün istiqamətləndirir.\n- Daimi təmiz su mənbəyi: Su qabları həmişə təmiz və dolu saxlanılmalıdır.",
                    ImageUrl = "/images/blog3.jpg",
                    AuthorName = "Murad Əliyev",
                    CreatedDate = new DateTime(2025, 2, 22, 15, 0, 0, DateTimeKind.Utc)
                },
                new BlogPost
                {
                    Id = 4,
                    Title = "Ev heyvanları üçün düzgün qidalanma qaydaları",
                    Slug = "ev-heyvanlari-ucun-duzgun-qidalanma-qaydalari",
                    Summary = "İt və pişiklərin yaş və çəkisinə uyğun düzgün rasion seçilməsi və vitamin balansının qorunması haqqında məlumat.",
                    Content = "Düzgün qidalanma ev heyvanlarının parlaq tük örtüyü, möhkəm dişləri və güclü immuniteti üçün vacibdir.\n\n1. Premium yemlərin üstünlükləri: Yüksək keyfiyyətli quru yemlər təbii ət mənbələri, zəruri minerallar və vitaminlərlə zəngindir.\n2. Yaş həddinə görə ayrılma: Balalar (Puppy/Kitten) üçün yüksək kalorili və kalsiumlu yemlər, yetkin heyvanlar üçün isə balanslı rasion tələb olunur.\n3. İnsan yeməklərinin zərərləri: Şokolad, soğan, sarımsaq, şirniyyat və ədviyyatlı yeməklər heyvanların qaraciyər və mədəsinə ciddi zərər vura bilər.\n4. Mövsümi vitamin əlavələri: Xüsusilə tük tökülməsi dövründə baytar məsləhəti ilə Omega-3 və multivitaminlər qəbul edilməlidir.",
                    ImageUrl = "/images/blog1.jpg",
                    AuthorName = "Veterinar Elmir",
                    CreatedDate = new DateTime(2025, 3, 1, 9, 15, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}
