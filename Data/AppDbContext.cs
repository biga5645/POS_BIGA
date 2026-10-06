using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using POS_BIGA.Models;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace POS_BIGA.Data
{
    /// <summary>
    /// 🏗️ قاعدة بيانات POS BIGA
    /// تصميم متوافق مع الـ Models الحالية مع تحسينات احترافية
    /// </summary>
    public class AppDbContext : DbContext
    {
        private const string FallbackConnectionString = "Server=localhost\\SQLEXPRESS;Database=POS_BIGA;Trusted_Connection=True;TrustServerCertificate=True;";

        public AppDbContext()
        {
            try
            {
                // تطبيق الـ Migrations
                Database.Migrate();
            }
            catch (Exception ex)
            {
                // إشعار المطور فقط - لا نستدعي EnsureCreated لأنه يتعارض مع Migrate
                try
                {
                    MessageBox.Show($"خطأ أثناء تحديث قاعدة البيانات: {ex.Message}",
                        "Database Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch { }
            }
        }

        // DbSets
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<AppSetting> AppSettings { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Credit> Credits { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<AppUser> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var connectionString = ResolveConnectionString();

                // Connection string مع Retry logic
                optionsBuilder.UseSqlServer(
                    connectionString,
                    options => options.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null
                    )
                );

#if DEBUG
                optionsBuilder.EnableSensitiveDataLogging();
                optionsBuilder.EnableDetailedErrors();
#endif

                // Suppress pending model changes warning
                optionsBuilder.ConfigureWarnings(warnings =>
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            }
        }

        private static string ResolveConnectionString()
        {
            var fromEnvironment = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
            if (!string.IsNullOrWhiteSpace(fromEnvironment))
                return fromEnvironment;

            var currentDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            var baseDirectoryPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            var projectDirectoryPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "appsettings.json"));

            var fromFile = ReadConnectionString(currentDirectoryPath)
                           ?? ReadConnectionString(baseDirectoryPath)
                           ?? ReadConnectionString(projectDirectoryPath);

            return string.IsNullOrWhiteSpace(fromFile) ? FallbackConnectionString : fromFile;
        }

        private static string? ReadConnectionString(string appSettingsPath)
        {
            try
            {
                if (!File.Exists(appSettingsPath))
                    return null;

                using var stream = File.OpenRead(appSettingsPath);
                using var document = JsonDocument.Parse(stream);

                if (!document.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings))
                    return null;

                if (!connectionStrings.TryGetProperty("DefaultConnection", out var defaultConnection))
                    return null;

                var value = defaultConnection.GetString();
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
            catch
            {
                return null;
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ======================================
            // 👤 Customer Configuration
            // ======================================
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.Property(c => c.FullName).IsRequired().HasMaxLength(200);
                entity.Property(c => c.Phone).HasMaxLength(20).IsRequired(false);
                entity.Property(c => c.Email).HasMaxLength(200).IsRequired(false);
                entity.Property(c => c.Address).HasMaxLength(500).IsRequired(false);
                entity.Property(c => c.Notes).HasMaxLength(2000).IsRequired(false);
                entity.Property(c => c.CreditLimit).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                entity.Property(c => c.IsActive).HasDefaultValue(true);
                entity.Property(c => c.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(c => c.CreatedBy).HasMaxLength(100);

                // Indexes
                entity.HasIndex(c => c.Phone);
                entity.HasIndex(c => c.IsActive);

                // Relationships
                entity.HasMany(c => c.Sales)
                      .WithOne(s => s.Customer)
                      .HasForeignKey(s => s.CustomerId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(c => c.Credits)
                      .WithOne(cr => cr.Customer)
                      .HasForeignKey(cr => cr.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ======================================
            // 🛒 Sale Configuration
            // ======================================
            modelBuilder.Entity<Sale>(entity =>
            {
                entity.Property(s => s.SaleNumber).HasMaxLength(50);
                entity.Property(s => s.SubTotal).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                entity.Property(s => s.TaxAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                entity.Property(s => s.TaxRate).HasColumnType("decimal(5,2)").HasDefaultValue(0);
                entity.Property(s => s.DiscountAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                entity.Property(s => s.DiscountPercent).HasColumnType("decimal(5,2)").HasDefaultValue(0);
                entity.Property(s => s.TotalAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                entity.Property(s => s.PaidAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                entity.Property(s => s.RemainingAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                entity.Property(s => s.PaymentType).HasMaxLength(50).HasDefaultValue("Cash");
                entity.Property(s => s.Status).HasMaxLength(50).HasDefaultValue("Completed");
                entity.Property(s => s.Notes).HasMaxLength(500);
                entity.Property(s => s.Cashier).HasMaxLength(100);
                entity.Property(s => s.RegisterId).HasMaxLength(50);
                entity.Property(s => s.CreatedBy).HasMaxLength(100);

                // Indexes
                entity.HasIndex(s => s.SaleNumber).IsUnique();
                entity.HasIndex(s => s.CustomerId);
                entity.HasIndex(s => s.SaleDate);
                entity.HasIndex(s => s.PaymentType);
                entity.HasIndex(s => s.Status);

                // Relationships
                entity.HasMany(s => s.Items)
                      .WithOne(i => i.Sale)
                      .HasForeignKey(i => i.SaleId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(s => s.Credits)
                      .WithOne(c => c.Sale)
                      .HasForeignKey(c => c.SaleId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ======================================
            // 📦 SaleItem Configuration
            // ======================================
            modelBuilder.Entity<SaleItem>(entity =>
            {
                entity.Property(i => i.ProductName).IsRequired().HasMaxLength(200);
                entity.Property(i => i.ProductBarcode).HasMaxLength(100);
                entity.Property(i => i.UnitName).HasMaxLength(50);
                entity.Property(i => i.Quantity).HasColumnType("decimal(18,3)").IsRequired();
                entity.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(i => i.LineTotal).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(i => i.DiscountAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                entity.Property(i => i.DiscountPercent).HasColumnType("decimal(5,2)").HasDefaultValue(0);
                entity.Property(i => i.LineTotalAfterDiscount).HasColumnType("decimal(18,2)").IsRequired();

                // Indexes
                entity.HasIndex(i => i.SaleId);
                entity.HasIndex(i => i.ProductId);

                // Relationships
                entity.HasOne(i => i.Product)
                      .WithMany()
                      .HasForeignKey(i => i.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ======================================
            // 💳 Credit Configuration
            // ======================================
            modelBuilder.Entity<Credit>(entity =>
            {
                entity.Property(c => c.OriginalAmount).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(c => c.PaidAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                entity.Property(c => c.RemainingAmount).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(c => c.Status).IsRequired();
                entity.Property(c => c.Notes).HasMaxLength(500);
                entity.Property(c => c.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(c => c.CreatedBy).HasMaxLength(100);

                // Indexes
                entity.HasIndex(c => c.CustomerId);
                entity.HasIndex(c => c.SaleId);
                entity.HasIndex(c => c.Status);
                entity.HasIndex(c => c.DueDate);

                // Relationships
                entity.HasMany(c => c.Payments)
                      .WithOne(p => p.Credit)
                      .HasForeignKey(p => p.CreditId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ======================================
            // 💵 Payment Configuration
            // ======================================
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.Property(p => p.Amount).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(p => p.PaymentMethod).IsRequired();
                entity.Property(p => p.PaymentDate).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(p => p.ReferenceNumber).HasMaxLength(100);
                entity.Property(p => p.Notes).HasMaxLength(500);
                entity.Property(p => p.ReceivedBy).HasMaxLength(100);

                // Indexes
                entity.HasIndex(p => p.CreditId);
                entity.HasIndex(p => p.PaymentDate);
                entity.HasIndex(p => p.PaymentMethod);
            });

            // ======================================
            // 📦 Product Configuration
            // ======================================
            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(p => p.Name).IsRequired().HasMaxLength(250);
                entity.Property(p => p.Barcode).HasMaxLength(100);
                entity.Property(p => p.SKU).HasMaxLength(100);
                entity.Property(p => p.PurchasePrice).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                entity.Property(p => p.SalePrice).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(p => p.CurrentStock).HasColumnType("decimal(18,3)").HasDefaultValue(0);
                entity.Property(p => p.MinimumStock).HasColumnType("decimal(18,3)").HasDefaultValue(0);
                entity.Property(p => p.Description).HasMaxLength(2000);
                entity.Property(p => p.ImageUrl).HasMaxLength(500);
                // ✅ لا حاجة لـ Unit - نعتمد فقط على UnitId
                entity.Property(p => p.IsActive).HasDefaultValue(true);
                entity.Property(p => p.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(p => p.CreatedBy).HasMaxLength(100);

                // Indexes
                // فهرس فريد على الباركود: يمنع تكرار نفس الباركود بين منتجين مختلفين
                // (باستثناء الباركود الفارغ "" الذي يُسمح بتكراره لأنه يعني عدم وجود باركود بعد)
                entity.HasIndex(p => p.Barcode)
                      .IsUnique()
                      .HasFilter("[Barcode] <> ''");
                entity.HasIndex(p => p.SKU);
                entity.HasIndex(p => p.Name);
                entity.HasIndex(p => p.CategoryId);
                entity.HasIndex(p => p.IsActive);

                // Relationships
                entity.HasOne(p => p.Category)
                      .WithMany(c => c.Products)
                      .HasForeignKey(p => p.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.UnitEntity)
                      .WithMany()
                      .HasForeignKey(p => p.UnitId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(p => p.StockMovements)
                      .WithOne(sm => sm.Product)
                      .HasForeignKey(sm => sm.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Seed Data
                entity.HasData(
                    new Product { Id = 1, Name = "قلم تسجيل", Barcode = "1001", SKU = "PEN-001", PurchasePrice = 0.90m, SalePrice = 1.50m, CurrentStock = 50m, MinimumStock = 10m, CategoryId = 1, UnitId = 1, ImageUrl = "https://via.placeholder.com/150?text=Pen", Description = "قلم تسجيل عالي الجودة", IsActive = true, CreatedBy = "System", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Product { Id = 2, Name = "دفتر ملاحظات", Barcode = "1002", SKU = "NTB-001", PurchasePrice = 2.20m, SalePrice = 3.75m, CurrentStock = 30m, MinimumStock = 5m, CategoryId = 1, UnitId = 1, ImageUrl = "https://via.placeholder.com/150?text=Notebook", Description = "دفتر ملاحظات A5", IsActive = true, CreatedBy = "System", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Product { Id = 3, Name = "زجاجة ماء", Barcode = "1003", SKU = "WTR-001", PurchasePrice = 0.40m, SalePrice = 0.90m, CurrentStock = 100m, MinimumStock = 20m, CategoryId = 2, UnitId = 10, ImageUrl = "https://via.placeholder.com/150?text=Water", Description = "ماء معدني 500 مل", IsActive = true, CreatedBy = "System", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Product { Id = 4, Name = "ممحاة", Barcode = "1004", SKU = "ERS-001", PurchasePrice = 0.20m, SalePrice = 0.50m, CurrentStock = 80m, MinimumStock = 15m, CategoryId = 1, UnitId = 1, ImageUrl = "https://via.placeholder.com/150?text=Eraser", Description = "ممحاة ناعمة", IsActive = true, CreatedBy = "System", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Product { Id = 5, Name = "سكين مكتبي", Barcode = "1005", SKU = "KNF-001", PurchasePrice = 1.50m, SalePrice = 2.20m, CurrentStock = 20m, MinimumStock = 5m, CategoryId = 3, UnitId = 1, ImageUrl = "https://via.placeholder.com/150?text=Knife", Description = "سكين مكتبي معدني", IsActive = true, CreatedBy = "System", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
                );
            });

            // ======================================
            // 📊 StockMovement Configuration
            // ======================================
            modelBuilder.Entity<StockMovement>(entity =>
            {
                entity.Property(sm => sm.MovementType).IsRequired().HasMaxLength(50);
                entity.Property(sm => sm.Quantity).HasColumnType("decimal(18,3)").IsRequired();
                entity.Property(sm => sm.ReferenceType).HasMaxLength(50);
                entity.Property(sm => sm.QuantityBefore).HasColumnType("decimal(18,3)").IsRequired();
                entity.Property(sm => sm.QuantityAfter).HasColumnType("decimal(18,3)").IsRequired();
                entity.Property(sm => sm.MovementDate).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(sm => sm.Reason).HasMaxLength(500);
                entity.Property(sm => sm.PerformedBy).HasMaxLength(100);
                entity.Property(sm => sm.Notes).HasMaxLength(500);

                // Indexes
                entity.HasIndex(sm => sm.ProductId);
                entity.HasIndex(sm => sm.MovementType);
                entity.HasIndex(sm => sm.MovementDate);
                entity.HasIndex(sm => new { sm.ReferenceType, sm.ReferenceId });

                // Relationships
                entity.HasOne(sm => sm.RelatedSale)
                      .WithMany()
                      .HasForeignKey(sm => sm.SaleId)
                      .OnDelete(DeleteBehavior.NoAction)
                      .IsRequired(false);
            });

            // ======================================
            // 📂 Category Configuration
            // ======================================
            modelBuilder.Entity<Category>(entity =>
            {
                entity.Property(c => c.Name).IsRequired().HasMaxLength(200);
                entity.Property(c => c.Description).HasMaxLength(500);
                entity.Property(c => c.ImageUrl).HasMaxLength(500);
                entity.Property(c => c.IsActive).HasDefaultValue(true);
                entity.Property(c => c.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Self-referencing for hierarchy
                entity.HasOne(c => c.ParentCategory)
                      .WithMany(c => c.SubCategories)
                      .HasForeignKey(c => c.ParentCategoryId)
                      .OnDelete(DeleteBehavior.NoAction);

                // Indexes
                entity.HasIndex(c => c.ParentCategoryId);
                entity.HasIndex(c => c.IsActive);

                // Seed Data
                entity.HasData(
                    new Category { Id = 1, Name = "مواد غدائية", Description = "مواد غذائية متنوعة", ImageUrl = "", IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Category { Id = 2, Name = "مشروبات", Description = "مشروبات ساخنة وباردة", ImageUrl = "", IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Category { Id = 3, Name = "مكتب", Description = "أدوات مكتبية", ImageUrl = "", IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
                );
            });

            // ======================================
            // 📏 Unit Configuration
            // ======================================
            modelBuilder.Entity<Unit>(entity =>
            {
                entity.Property(u => u.Name).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Abbreviation).HasMaxLength(20);
                entity.Property(u => u.IsActive).HasDefaultValue(true);
                entity.Property(u => u.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                // ✨ خصائص التجزئة
                entity.Property(u => u.IsDivisible).HasDefaultValue(false);
                entity.Property(u => u.SubUnitName).HasMaxLength(100).IsRequired(false);
                entity.Property(u => u.SubUnitQuantity).HasColumnType("decimal(18,3)").HasDefaultValue(1);

                // ✨ خاصية السماح بالثمن المخصص
                entity.Property(u => u.AllowCustomPrice).HasDefaultValue(false);

                // ✨ Seed Data - 12 وحدة شائعة
                entity.HasData(
                    new Unit { Id = 1, Name = "كيلوجرام", Abbreviation = "kg", IsActive = true, IsDivisible = true, SubUnitName = "غرام", SubUnitQuantity = 1000m, AllowCustomPrice = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Unit { Id = 2, Name = "غرام", Abbreviation = "g", IsActive = true, IsDivisible = false, SubUnitQuantity = 1m, AllowCustomPrice = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Unit { Id = 3, Name = "متر", Abbreviation = "m", IsActive = true, IsDivisible = true, SubUnitName = "سنتيمتر", SubUnitQuantity = 100m, AllowCustomPrice = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Unit { Id = 4, Name = "لتر", Abbreviation = "L", IsActive = true, IsDivisible = true, SubUnitName = "مليلتر", SubUnitQuantity = 1000m, AllowCustomPrice = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Unit { Id = 5, Name = "قطعة", Abbreviation = "piece", IsActive = true, IsDivisible = false, SubUnitQuantity = 1m, AllowCustomPrice = false, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Unit { Id = 6, Name = "كرتونة", Abbreviation = "box", IsActive = true, IsDivisible = true, SubUnitName = "قطعة", SubUnitQuantity = 12m, AllowCustomPrice = false, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Unit { Id = 7, Name = "دزينة", Abbreviation = "dozen", IsActive = true, IsDivisible = true, SubUnitName = "قطعة", SubUnitQuantity = 12m, AllowCustomPrice = false, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Unit { Id = 8, Name = "علبة", Abbreviation = "can", IsActive = true, IsDivisible = true, SubUnitName = "قطعة", SubUnitQuantity = 24m, AllowCustomPrice = false, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Unit { Id = 9, Name = "طن", Abbreviation = "ton", IsActive = true, IsDivisible = true, SubUnitName = "كيلوجرام", SubUnitQuantity = 1000m, AllowCustomPrice = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Unit { Id = 10, Name = "زجاجة", Abbreviation = "btl", IsActive = true, IsDivisible = false, SubUnitQuantity = 1m, AllowCustomPrice = false, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Unit { Id = 11, Name = "حبة", Abbreviation = "unit", IsActive = true, IsDivisible = false, SubUnitQuantity = 1m, AllowCustomPrice = false, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Unit { Id = 12, Name = "كيس", Abbreviation = "bag", IsActive = true, IsDivisible = true, SubUnitName = "قطعة", SubUnitQuantity = 50m, AllowCustomPrice = false, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
                );
            });

            // ======================================
            // ⚙️ AppSetting Configuration
            // ======================================
            modelBuilder.Entity<AppSetting>(entity =>
            {
                entity.Property(s => s.SettingKey).IsRequired().HasMaxLength(100);
                entity.Property(s => s.SettingValue).HasMaxLength(4000);
                entity.Property(s => s.SettingType).HasMaxLength(50);
                entity.Property(s => s.Description).HasMaxLength(500);
                entity.Property(s => s.Category).HasMaxLength(100);
                entity.Property(s => s.IsEditable).HasDefaultValue(true);
                entity.Property(s => s.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.Property(s => s.ShopName).HasDefaultValue("");
                entity.Property(s => s.ShopAddress).HasDefaultValue("");
                entity.Property(s => s.ShopPhone).HasDefaultValue("");
                entity.Property(s => s.TaxRate).HasColumnType("decimal(5,2)").HasDefaultValue(0);
                entity.Property(s => s.Currency).HasDefaultValue("MAD");
                entity.Property(s => s.CreditEnabled).HasDefaultValue(false);
                entity.Property(s => s.CreditLimit).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                entity.Property(s => s.InvoiceMessage).HasDefaultValue("");

                // Unique Index
                entity.HasIndex(s => s.SettingKey).IsUnique();
                entity.HasIndex(s => s.Category);

                // Seed Data
                entity.HasData(
                    new AppSetting
                    {
                        Id = 1,
                        SettingKey = "ShopName",
                        SettingValue = "متجري",
                        SettingType = "String",
                        Description = "اسم المتجر",
                        Category = "General",
                        IsEditable = true,
                        ShopName = "متجري",
                        Currency = "MAD",
                        CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    },
                    new AppSetting
                    {
                        Id = 2,
                        SettingKey = "TaxRate",
                        SettingValue = "0.00",
                        SettingType = "Decimal",
                        Description = "نسبة الضريبة الافتراضية",
                        Category = "Sales",
                        IsEditable = true,
                        TaxRate = 0m,
                        Currency = "MAD",
                        CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    },
                    new AppSetting
                    {
                        Id = 3,
                        SettingKey = "ReceiptPrinter",
                        SettingValue = "",
                        SettingType = "String",
                        Description = "الطابعة الافتراضية للإيصالات",
                        Category = "Hardware",
                        IsEditable = true,
                        Currency = "MAD",
                        CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    }
                );
            });

            // ======================================
            // 👤 Customer Seed Data
            // ======================================
            modelBuilder.Entity<Customer>().HasData(
                new Customer
                {
                    Id = 1,
                    FullName = "عميل تجريبي",
                    Phone = "0612345678",
                    Email = "test@example.com",
                    Address = "الرباط",
                    Notes = "زبون تجريبي",
                    CreditLimit = 1000m,
                    IsActive = true,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            // ======================================
            // 💳 Credit Seed Data
            // ======================================
            modelBuilder.Entity<Credit>().HasData(
                new Credit
                {
                    Id = 1,
                    CustomerId = 1,
                    OriginalAmount = 250m,
                    PaidAmount = 0m,
                    RemainingAmount = 250m,
                    Status = CreditStatus.Unpaid,
                    Notes = " دين تجريبي أولي",
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            // ======================================
            // 👤 AppUser Configuration
            // ======================================
            modelBuilder.Entity<AppUser>(entity =>
            {
                entity.Property(u => u.Username).IsRequired().HasMaxLength(100);
                entity.Property(u => u.FullName).IsRequired().HasMaxLength(150);
                entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(256);
                entity.Property(u => u.Pin).HasMaxLength(20).IsRequired(false);
                entity.Property(u => u.IsActive).HasDefaultValue(true);
                entity.Property(u => u.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(u => u.CreatedBy).HasMaxLength(100);

                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.IsActive);

                // Seed Data - Admin افتراضي (كلمة المرور: admin123)
                entity.HasData(new AppUser
                {
                    Id = 1,
                    Username = "admin",
                    FullName = "مدير النظام ",
                    PasswordHash = "$2a$11$K7JUGRrFxl0A4eQ.RiSWxeqvXGMsBMPPHPGfz8rGFiJi4sCoqQ3Oy",
                    Role = UserRole.Admin,
                    IsActive = true,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                });
            });
        }

        // ======================================
        // 🛠️ Helper Methods
        // ======================================

        /// <summary>
        /// الحصول على إعداد بالمفتاح
        /// </summary>
        public async Task<AppSetting> GetSettingAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;
            return await AppSettings.FirstOrDefaultAsync(s => s.SettingKey == key);
        }

        /// <summary>
        /// الحصول على قيمة إعداد كنص
        /// </summary>
        public async Task<string> GetSettingValueAsync(string key, string defaultValue = null)
        {
            var s = await GetSettingAsync(key);
            return s != null ? s.Value : defaultValue;
        }

        /// <summary>
        /// حفظ قيمة إعداد
        /// </summary>
        public async Task SetSettingValueAsync(string key, string value, string description = null)
        {
            if (string.IsNullOrWhiteSpace(key)) return;

            var s = await GetSettingAsync(key);
            if (s == null)
            {
                s = new AppSetting
                {
                    SettingKey = key,
                    SettingValue = value ?? string.Empty,
                    SettingType = "String",
                    Description = description ?? string.Empty,
                    Category = "General",
                    CreatedAt = DateTime.UtcNow
                };
                AppSettings.Add(s);
            }
            else
            {
                s.SettingValue = value ?? string.Empty;
                if (!string.IsNullOrEmpty(description)) s.Description = description;
                s.UpdatedAt = DateTime.UtcNow;
                AppSettings.Update(s);
            }

            await SaveChangesAsync();
        }

        /// <summary>
        /// الحصول على إعدادات المتجر
        /// </summary>
        public async Task<AppSetting> GetStoreSettingsAsync()
        {
            var s = await AppSettings.FirstOrDefaultAsync();
            if (s == null)
            {
                s = new AppSetting
                {
                    ShopName = "متجري",
                    ShopAddress = string.Empty,
                    ShopPhone = string.Empty,
                    TaxRate = 0m,
                    Currency = "MAD",
                    CreditEnabled = false,
                    CreditLimit = 0m,
                    InvoiceMessage = string.Empty
                };
            }
            return s;
        }

        /// <summary>
        /// حفظ إعدادات المتجر
        /// </summary>
        public async Task SaveStoreSettingsAsync(AppSetting settings)
        {
            if (settings == null) return;

            var s = await AppSettings.FirstOrDefaultAsync();
            if (s == null)
            {
                AppSettings.Add(settings);
            }
            else
            {
                // نسخ الحقول
                s.ShopName = settings.ShopName;
                s.ShopAddress = settings.ShopAddress;
                s.ShopPhone = settings.ShopPhone;
                s.TaxRate = settings.TaxRate;
                s.Currency = settings.Currency;
                s.CreditEnabled = settings.CreditEnabled;
                s.CreditLimit = settings.CreditLimit;
                s.InvoiceMessage = settings.InvoiceMessage;
                AppSettings.Update(s);
            }

            await SaveChangesAsync();
        }
    }
}