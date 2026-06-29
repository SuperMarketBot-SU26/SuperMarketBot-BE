using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Domain.Entities;

namespace SmartMarketBot.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    // ── Region 1: Customer & Identity ────────────────────────────────────────
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<HealthTag> HealthTags => Set<HealthTag>();
    public DbSet<MemberHealthPreference> MemberHealthPreferences => Set<MemberHealthPreference>();
    public DbSet<MemberNotification> MemberNotifications => Set<MemberNotification>();

    // ── Region 2: Product Catalog ────────────────────────────────────────────
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Subcategory> Subcategories => Set<Subcategory>();
    public DbSet<ProductType> ProductTypes => Set<ProductType>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductHealthTag> ProductHealthTags => Set<ProductHealthTag>();

    // ── Region 3: Shopping & Meal ────────────────────────────────────────────
    public DbSet<InvoiceHistory> InvoiceHistories => Set<InvoiceHistory>();
    public DbSet<InvoiceHistoryItem> InvoiceHistoryItems => Set<InvoiceHistoryItem>();
    public DbSet<MealSuggestion> MealSuggestions => Set<MealSuggestion>();
    public DbSet<MealItem> MealItems => Set<MealItem>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    // ── Region 4: Store Layout ───────────────────────────────────────────────
    public DbSet<Floor> Floors => Set<Floor>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<Aisle> Aisles => Set<Aisle>();
    public DbSet<Shelf> Shelves => Set<Shelf>();
    public DbSet<Slot> Slots => Set<Slot>();
    public DbSet<ProductSlot> ProductSlots => Set<ProductSlot>();

    // ── Region 5: Ad & Sponsorship ───────────────────────────────────────────
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<AdPackage> AdPackages => Set<AdPackage>();
    public DbSet<AdCampaign> AdCampaigns => Set<AdCampaign>();
    public DbSet<SponsoredProduct> SponsoredProducts => Set<SponsoredProduct>();
    public DbSet<AdCampaignLog> AdCampaignLogs => Set<AdCampaignLog>();
    public DbSet<AdResource> AdResources => Set<AdResource>();

    // ── Region 6: Robot & Navigation ─────────────────────────────────────────
    public DbSet<Robot> Robots => Set<Robot>();
    public DbSet<RobotLog> RobotLogs => Set<RobotLog>();
    public DbSet<RobotZone> RobotZones => Set<RobotZone>();
    public DbSet<Map> Maps => Set<Map>();
    public DbSet<NavigationNode> NavigationNodes => Set<NavigationNode>();
    public DbSet<NavigationEdge> NavigationEdges => Set<NavigationEdge>();
    public DbSet<AisleNode> AisleNodes => Set<AisleNode>();
    public DbSet<RobotRoute> RobotRoutes => Set<RobotRoute>();
    public DbSet<RouteNodeMapping> RouteNodeMappings => Set<RouteNodeMapping>();
    public DbSet<RouteAssignment> RouteAssignments => Set<RouteAssignment>();
    public DbSet<AisleScan> AisleScans => Set<AisleScan>();
    public DbSet<SemanticObject> SemanticObjects => Set<SemanticObject>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ═══════════════════════════════════════════════════════════════════════
        // ERD V4.0 — Direct Schema Mapping
        // DB chuẩn: db/erd_database.sql (37 bảng, gộp EMAIL_OTP vào ACCOUNT)
        // Default thời gian: DATEADD(hour, 7, GETUTCDATE()) — UTC+7 (VN)
        // ═══════════════════════════════════════════════════════════════════════

        // ─────────────────────────────────────────────
        // REGION 1: Customer & Identity
        // ─────────────────────────────────────────────

        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("ACCOUNT");
            entity.HasKey(x => x.AccountId);
            entity.Property(x => x.AccountId).HasColumnName("AccountID");
            entity.Property(x => x.Username).HasColumnName("Username").HasMaxLength(100).IsRequired();
            entity.Property(x => x.PasswordHash).HasColumnName("PasswordHash").HasMaxLength(500).IsRequired();
            entity.Property(x => x.Email).HasColumnName("Email").HasMaxLength(256).IsRequired();
            entity.Property(x => x.Phone).HasColumnName("Phone").HasMaxLength(20);
            entity.Property(x => x.FullName).HasColumnName("FullName").HasMaxLength(100);
            entity.Property(x => x.AvatarUrl).HasColumnName("AvatarUrl").HasMaxLength(500);
            entity.Property(x => x.Status).HasColumnName("Status").HasMaxLength(50).IsRequired();
            entity.Property(x => x.Role).HasColumnName("Role").HasMaxLength(50).IsRequired();
            entity.Property(x => x.OtpCode).HasColumnName("OtpCode").HasMaxLength(6);
            entity.Property(x => x.OtpExpiredAt).HasColumnName("OtpExpiredAt");
            entity.Property(x => x.OtpType).HasColumnName("OtpType").HasMaxLength(50);
            entity.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("DATEADD(hour, 7, GETUTCDATE())");
            entity.Property(x => x.RefreshToken).HasColumnName("RefreshToken").HasMaxLength(500);
            entity.Property(x => x.RefreshExpiry).HasColumnName("RefreshExpiry");
            entity.Property(x => x.IsTokenRevoked).HasColumnName("IsTokenRevoked").HasDefaultValue(false);
            entity.HasIndex(x => x.Username).IsUnique().HasDatabaseName("IX_ACCOUNT_Username");
            entity.HasIndex(x => x.Email).IsUnique().HasDatabaseName("IX_ACCOUNT_Email");
        });

        modelBuilder.Entity<Member>(entity =>
        {
            entity.ToTable("MEMBER");
            entity.HasKey(x => x.MemberId);
            entity.Property(x => x.MemberId).HasColumnName("MemberID");
            entity.Property(x => x.AccountId).HasColumnName("AccountID");
            entity.Property(x => x.FullName).HasColumnName("FullName").HasMaxLength(200).IsRequired();
            entity.Property(x => x.FacePath).HasColumnName("FacePath").HasMaxLength(500);
            entity.Property(x => x.FaceVector).HasColumnName("FaceVector").HasColumnType("nvarchar(max)");
            entity.Property(x => x.SpendingLimit).HasColumnName("SpendingLimit").HasPrecision(18, 2);
            entity.Property(x => x.TotalPoints).HasColumnName("TotalPoints").HasDefaultValue(0);
            // 1-1 nullable: Account.Member back-navigation prevents shadow FK
            entity.HasOne(x => x.Account)
                .WithOne(a => a.Member)
                .HasForeignKey<Member>(x => x.AccountId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Membership>(entity =>
        {
            entity.ToTable("MEMBERSHIP");
            entity.HasKey(x => x.MembershipId);
            entity.Property(x => x.MembershipId).HasColumnName("MembershipID");
            entity.Property(x => x.MemberId).HasColumnName("MemberID");
            entity.Property(x => x.TierName).HasColumnName("TierName").HasMaxLength(50).IsRequired();
            entity.Property(x => x.Status).HasColumnName("Status").HasMaxLength(50).IsRequired();
            entity.HasOne(x => x.Member)
                .WithMany(m => m.Memberships)
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HealthTag>(entity =>
        {
            entity.ToTable("HEALTH_TAG");
            entity.HasKey(x => x.HealthTagId);
            entity.Property(x => x.HealthTagId).HasColumnName("HealthTagID");
            entity.Property(x => x.TagName).HasColumnName("TagName").HasMaxLength(100).IsRequired();
            entity.Property(x => x.TagType).HasColumnName("TagType").HasMaxLength(50).IsRequired().HasDefaultValue("diet");
        });

        modelBuilder.Entity<MemberHealthPreference>(entity =>
        {
            entity.ToTable("MEMBERHEALTH_PREFERENCE");
            entity.HasKey(x => new { x.MemberId, x.HealthTagId });
            entity.Property(x => x.MemberId).HasColumnName("MemberID");
            entity.Property(x => x.HealthTagId).HasColumnName("HealthTagID");
            entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
            entity.HasOne(x => x.Member)
                .WithMany(m => m.MemberHealthPreferences)
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.HealthTag)
                .WithMany(h => h.MemberHealthPreferences)
                .HasForeignKey(x => x.HealthTagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Refresh token đã gộp vào bảng ACCOUNT (37 bảng — không có USER_TOKEN).

        modelBuilder.Entity<MemberNotification>(entity =>
        {
            entity.ToTable("MEMBER_NOTIFICATION");
            entity.HasKey(x => x.NotificationId);
            entity.Property(x => x.NotificationId).HasColumnName("NotificationID");
            entity.Property(x => x.MemberId).HasColumnName("MemberID");
            entity.Property(x => x.NotifType).HasColumnName("NotifType").HasMaxLength(50).IsRequired();
            entity.Property(x => x.Title).HasColumnName("Title").HasMaxLength(200).IsRequired();
            entity.Property(x => x.Message).HasColumnName("Message").HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(x => x.PayloadJson).HasColumnName("PayloadJson").HasColumnType("nvarchar(max)");
            entity.Property(x => x.IsRead).HasColumnName("IsRead").HasDefaultValue(false);
            entity.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("DATEADD(hour, 7, GETUTCDATE())");
            entity.HasOne(x => x.Member)
                .WithMany(m => m.MemberNotifications)
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.MemberId, x.IsRead }).HasDatabaseName("IX_MN_MemberID_IsRead");
            entity.HasIndex(x => x.CreatedAt).HasDatabaseName("IX_MN_CreatedAt").IsDescending();
        });

        // MemberHealthPreference đã cấu hình ở trên

        // ─────────────────────────────────────────────
        // REGION 2: Product Catalog
        // ─────────────────────────────────────────────

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("CATEGORY");
            entity.HasKey(x => x.CategoryId);
            entity.Property(x => x.CategoryId).HasColumnName("CategoryID");
            entity.Property(x => x.CategoryName).HasColumnName("CategoryName").HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Subcategory>(entity =>
        {
            entity.ToTable("SUBCATEGORY");
            entity.HasKey(x => x.SubcategoryId);
            entity.Property(x => x.SubcategoryId).HasColumnName("SubcategoryID");
            entity.Property(x => x.CategoryId).HasColumnName("CategoryID");
            entity.Property(x => x.SubcategoryName).HasColumnName("SubcategoryName").HasMaxLength(100).IsRequired();
            entity.HasOne(x => x.Category)
                .WithMany(c => c.Subcategories)
                .HasForeignKey(x => x.CategoryId);
        });

        modelBuilder.Entity<ProductType>(entity =>
        {
            entity.ToTable("PRODUCT_TYPE");
            entity.HasKey(x => x.ProductTypeId);
            entity.Property(x => x.ProductTypeId).HasColumnName("ProductTypeID");
            entity.Property(x => x.SubcategoryId).HasColumnName("SubcategoryID");
            entity.Property(x => x.TypeName).HasColumnName("TypeName").HasMaxLength(100).IsRequired();
            entity.HasOne(x => x.Subcategory)
                .WithMany(s => s.ProductTypes)
                .HasForeignKey(x => x.SubcategoryId);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("PRODUCT");
            entity.HasKey(x => x.ProductId);
            entity.Property(x => x.ProductId).HasColumnName("ProductID");
            entity.Property(x => x.ProductTypeId).HasColumnName("ProductTypeID");
            entity.Property(x => x.ProductName).HasColumnName("ProductName").HasMaxLength(200).IsRequired();
            entity.Property(x => x.UnitPrice).HasColumnName("UnitPrice").HasPrecision(18, 2).HasDefaultValue(0m);
            entity.Property(x => x.PromotionPrice).HasColumnName("PromotionPrice").HasPrecision(18, 2);
            entity.Property(x => x.ExpiredDate).HasColumnName("ExpiredDate");
            entity.Property(x => x.ImageUrl).HasColumnName("ImageUrl").HasMaxLength(500);
            entity.Property(x => x.WeightOrVolume).HasColumnName("WeightOrVolume").HasPrecision(18, 3);
            entity.Property(x => x.Unit).HasColumnName("Unit").HasMaxLength(20);
            entity.Property(x => x.Description).HasColumnName("Description").HasColumnType("nvarchar(max)");
            entity.Property(x => x.Status).HasColumnName("Status").HasMaxLength(50).IsRequired();
            entity.Property(x => x.SubstituteProductId).HasColumnName("SubstituteProductID");
            entity.HasOne(x => x.ProductType)
                .WithMany(pt => pt.Products)
                .HasForeignKey(x => x.ProductTypeId);
            // Self-ref: substitute product — NoAction to prevent cascade cycle
            entity.HasOne(x => x.SubstituteProduct)
                .WithMany()
                .HasForeignKey(x => x.SubstituteProductId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<ProductHealthTag>(entity =>
        {
            entity.ToTable("PRODUCT_HEALTHTAG");
            entity.HasKey(x => new { x.ProductId, x.HealthTagId });
            entity.Property(x => x.ProductId).HasColumnName("ProductID");
            entity.Property(x => x.HealthTagId).HasColumnName("HealthTagID");
            entity.HasOne(x => x.Product)
                .WithMany(p => p.ProductHealthTags)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            // NoAction: chống multiple cascade paths — PRODUCT_HEALTHTAG đã có Cascade từ PRODUCT
            entity.HasOne(x => x.HealthTag)
                .WithMany(h => h.ProductHealthTags)
                .HasForeignKey(x => x.HealthTagId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ─────────────────────────────────────────────
        // REGION 3: Shopping & Meal
        // ─────────────────────────────────────────────

        modelBuilder.Entity<InvoiceHistory>(entity =>
        {
            entity.ToTable("INVOICE_HISTORY");
            entity.HasKey(x => x.InvoiceHistoryId);
            entity.Property(x => x.InvoiceHistoryId).HasColumnName("InvoiceHistoryID");
            entity.Property(x => x.MemberId).HasColumnName("MemberID");
            entity.Property(x => x.PurchaseDate).HasColumnName("PurchaseDate").HasDefaultValueSql("DATEADD(hour, 7, GETUTCDATE())");
            entity.Property(x => x.TotalPrice).HasColumnName("TotalPrice").HasPrecision(18, 2).HasDefaultValue(0m);
            entity.HasOne(x => x.Member)
                .WithMany(m => m.InvoiceHistories)
                .HasForeignKey(x => x.MemberId);
        });

        modelBuilder.Entity<InvoiceHistoryItem>(entity =>
        {
            entity.ToTable("INVOICE_HISTORY_ITEM");
            entity.HasKey(x => x.InvoiceHistoryItemId);
            entity.Property(x => x.InvoiceHistoryItemId).HasColumnName("InvoiceHistoryItemID");
            entity.Property(x => x.InvoiceHistoryId).HasColumnName("InvoiceHistoryID");
            entity.Property(x => x.ProductId).HasColumnName("ProductID");
            entity.Property(x => x.Quantity).HasColumnName("Quantity").HasDefaultValue(1);
            entity.Property(x => x.UnitPrice).HasColumnName("UnitPrice").HasPrecision(18, 2).IsRequired();
            entity.HasOne(x => x.InvoiceHistory)
                .WithMany(ih => ih.InvoiceHistoryItems)
                .HasForeignKey(x => x.InvoiceHistoryId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Product)
                .WithMany(p => p.InvoiceHistoryItems)
                .HasForeignKey(x => x.ProductId);
        });

        modelBuilder.Entity<MealSuggestion>(entity =>
        {
            entity.ToTable("MEAL_SUGGESTION");
            entity.HasKey(x => x.MealSuggestionId);
            entity.Property(x => x.MealSuggestionId).HasColumnName("MealSuggestionID");
            entity.Property(x => x.MealName).HasColumnName("MealName").HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasColumnName("Description").HasColumnType("nvarchar(max)");
            entity.Property(x => x.YieldPortions).HasColumnName("YieldPortions").HasDefaultValue(1);
            entity.Property(x => x.ImageUrl).HasColumnName("ImageUrl").HasMaxLength(500);
            entity.Property(x => x.Calories).HasColumnName("Calories");
            entity.Property(x => x.HealthyScore).HasColumnName("healthy_score");
            entity.Property(x => x.AlternativeSuggestion).HasColumnName("alternative_suggestion").HasMaxLength(500);
        });

        modelBuilder.Entity<MealItem>(entity =>
        {
            entity.ToTable("MEAL_ITEM");
            entity.HasKey(x => new { x.MealSuggestionId, x.ProductId });
            entity.Property(x => x.MealSuggestionId).HasColumnName("MealSuggestionID");
            entity.Property(x => x.ProductId).HasColumnName("ProductID");
            entity.Property(x => x.QuantityRequired).HasColumnName("QuantityRequired").HasPrecision(18, 3).HasDefaultValue(1m);
            entity.Property(x => x.UnitOfMeasure).HasColumnName("UnitOfMeasure").HasMaxLength(20);
            entity.HasOne(x => x.MealSuggestion)
                .WithMany(ms => ms.MealItems)
                .HasForeignKey(x => x.MealSuggestionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Product)
                .WithMany(p => p.MealItems)
                .HasForeignKey(x => x.ProductId);
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.ToTable("CART");
            entity.HasKey(x => x.CartId);
            entity.Property(x => x.CartId).HasColumnName("CartID");
            entity.Property(x => x.MemberId).HasColumnName("MemberID");
            entity.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("DATEADD(hour, 7, GETUTCDATE())");
            entity.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");

            entity.HasOne(x => x.Member)
                .WithOne()
                .HasForeignKey<Cart>(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.ToTable("CART_ITEM");
            entity.HasKey(x => x.CartItemId);
            entity.Property(x => x.CartItemId).HasColumnName("CartItemID");
            entity.Property(x => x.CartId).HasColumnName("CartID");
            entity.Property(x => x.ProductId).HasColumnName("ProductID");
            entity.Property(x => x.Quantity).HasColumnName("Quantity").HasDefaultValue(1);
            entity.Property(x => x.AddedAt).HasColumnName("AddedAt").HasDefaultValueSql("DATEADD(hour, 7, GETUTCDATE())");

            entity.HasOne(x => x.Cart)
                .WithMany(c => c.CartItems)
                .HasForeignKey(x => x.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            // NoAction: chống multiple cascade paths — CART_ITEM đã có Cascade từ CART
            entity.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ─────────────────────────────────────────────
        // REGION 4: Store Layout
        // ─────────────────────────────────────────────

        modelBuilder.Entity<Floor>(entity =>
        {
            entity.ToTable("FLOOR");
            entity.HasKey(x => x.FloorId);
            entity.Property(x => x.FloorId).HasColumnName("FloorID");
            entity.Property(x => x.FloorNumber).HasColumnName("FloorNumber").HasDefaultValue(1);
        });

        modelBuilder.Entity<Zone>(entity =>
        {
            entity.ToTable("ZONE");
            entity.HasKey(x => x.ZoneId);
            entity.Property(x => x.ZoneId).HasColumnName("ZoneID");
            entity.Property(x => x.FloorId).HasColumnName("FloorID");
            entity.Property(x => x.ZoneName).HasColumnName("ZoneName").HasMaxLength(100);
            entity.Property(x => x.Description).HasColumnName("Description").HasMaxLength(500);
            entity.HasOne(x => x.Floor)
                .WithMany(f => f.Zones)
                .HasForeignKey(x => x.FloorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Aisle>(entity =>
        {
            entity.ToTable("AISLE");
            entity.HasKey(x => x.AisleId);
            entity.Property(x => x.AisleId).HasColumnName("AisleID");
            entity.Property(x => x.ZoneId).HasColumnName("ZoneID");
            entity.Property(x => x.AisleCode).HasColumnName("AisleCode").HasMaxLength(20).IsRequired();
            entity.Property(x => x.AisleName).HasColumnName("AisleName").HasMaxLength(100);
            entity.HasOne(x => x.Zone)
                .WithMany(z => z.Aisles)
                .HasForeignKey(x => x.ZoneId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Shelf>(entity =>
        {
            entity.ToTable("SHELF");
            entity.HasKey(x => x.ShelfId);
            entity.Property(x => x.ShelfId).HasColumnName("ShelfID");
            entity.Property(x => x.AisleId).HasColumnName("AisleID");
            entity.Property(x => x.LevelNumber).HasColumnName("LevelNumber").HasDefaultValue(1);
            entity.HasOne(x => x.Aisle)
                .WithMany(a => a.Shelves)
                .HasForeignKey(x => x.AisleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Slot>(entity =>
        {
            entity.ToTable("SLOT");
            entity.HasKey(x => x.SlotId);
            entity.Property(x => x.SlotId).HasColumnName("SlotID");
            entity.Property(x => x.ShelfId).HasColumnName("ShelfID");
            entity.Property(x => x.SlotCode).HasColumnName("SlotCode").HasMaxLength(20);
            entity.Property(x => x.Quantity).HasColumnName("Quantity").HasDefaultValue(0);
            entity.Property(x => x.LastScannedAt).HasColumnName("LastScannedAt");
            entity.HasOne(x => x.Shelf)
                .WithMany(s => s.Slots)
                .HasForeignKey(x => x.ShelfId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProductSlot>(entity =>
        {
            entity.ToTable("PRODUCT_SLOT");
            entity.HasKey(x => x.ProductSlotId);
            entity.Property(x => x.ProductSlotId).HasColumnName("ProductsSlotID");
            entity.Property(x => x.SlotId).HasColumnName("SlotID");
            entity.Property(x => x.ProductId).HasColumnName("ProductID");
            entity.HasOne(x => x.Slot)
                .WithMany(s => s.ProductSlots)
                .HasForeignKey(x => x.SlotId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Product)
                .WithMany(p => p.ProductSlots)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.SlotId, x.ProductId }).HasDatabaseName("IX_PRODUCT_SLOT_slot_product");
        });

        // ─────────────────────────────────────────────
        // REGION 5: Ad & Sponsorship
        // ─────────────────────────────────────────────

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.ToTable("BRAND");
            entity.HasKey(x => x.BrandId);
            entity.Property(x => x.BrandId).HasColumnName("BrandID");
            entity.Property(x => x.BrandName).HasColumnName("BrandName").HasMaxLength(100).IsRequired();
            entity.Property(x => x.Wallet).HasColumnName("Wallet").HasPrecision(18, 2).HasDefaultValue(0m);
            entity.Property(x => x.Description).HasColumnName("Description").HasMaxLength(500);
        });

        modelBuilder.Entity<AdPackage>(entity =>
        {
            entity.ToTable("AD_PACKAGE");
            entity.HasKey(x => x.PackageId);
            entity.Property(x => x.PackageId).HasColumnName("PackageID");
            entity.Property(x => x.PackageName).HasColumnName("PackageName").HasMaxLength(100).IsRequired();
            entity.Property(x => x.PricePackage).HasColumnName("PricePackage").HasPrecision(18, 2).HasDefaultValue(0m);
            entity.Property(x => x.PriceRoute).HasColumnName("PriceRoute").HasPrecision(18, 2).HasDefaultValue(0m);
            entity.Property(x => x.BasePriceClick).HasColumnName("BasePriceClick").HasPrecision(18, 2).HasDefaultValue(0m);
            entity.Property(x => x.AdScore).HasColumnName("AdScore").HasDefaultValue(0);
            entity.Property(x => x.Status).HasColumnName("Status").HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<AdCampaign>(entity =>
        {
            entity.ToTable("AD_CAMPAIGN");
            entity.HasKey(x => x.AdCampaignId);
            entity.Property(x => x.AdCampaignId).HasColumnName("AdCampaignID");
            entity.Property(x => x.PackageId).HasColumnName("PackageID");
            entity.Property(x => x.BrandId).HasColumnName("BrandID");
            entity.Property(x => x.RobotZoneId).HasColumnName("RobotZoneID");
            entity.Property(x => x.CampaignName).HasColumnName("CampaignName").HasMaxLength(200).IsRequired();
            entity.Property(x => x.StartDate).HasColumnName("StartDate");
            entity.Property(x => x.EndDate).HasColumnName("EndDate");
            entity.Property(x => x.Status).HasColumnName("Status").HasMaxLength(50).IsRequired();
            // NoAction: chống cascade cycle
            entity.HasOne(x => x.Package)
                .WithMany(p => p.AdCampaigns)
                .HasForeignKey(x => x.PackageId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Brand)
                .WithMany(b => b.AdCampaigns)
                .HasForeignKey(x => x.BrandId)
                .OnDelete(DeleteBehavior.NoAction);
            // Nullable FK → RobotZone (SET NULL khi zone bị xoá)
            entity.HasOne(x => x.RobotZone)
                .WithMany()
                .HasForeignKey(x => x.RobotZoneId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SponsoredProduct>(entity =>
        {
            entity.ToTable("SPONSORED_PRODUCT");
            entity.HasKey(x => x.SponsoredId);
            entity.Property(x => x.SponsoredId).HasColumnName("SponsoredID");
            entity.Property(x => x.AdCampaignId).HasColumnName("AdCampaignID");
            entity.Property(x => x.ProductId).HasColumnName("ProductID");
            entity.Property(x => x.Priority).HasColumnName("Priority").HasDefaultValue(0);
            entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
            entity.HasOne(x => x.AdCampaign)
                .WithMany(ac => ac.SponsoredProducts)
                .HasForeignKey(x => x.AdCampaignId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Product)
                .WithMany(p => p.SponsoredProducts)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AdResource>(entity =>
        {
            entity.ToTable("AD_RESOURCE");
            entity.HasKey(x => x.ResourceId);
            entity.Property(x => x.ResourceId).HasColumnName("ResourceID");
            entity.Property(x => x.AdCampaignId).HasColumnName("AdCampaignID");
            entity.Property(x => x.ResourceType).HasColumnName("ResourceType").HasMaxLength(20).IsRequired();
            entity.Property(x => x.ResourceUrl).HasColumnName("ResourceURL").HasMaxLength(500).IsRequired();
            entity.Property(x => x.ContentText).HasColumnName("ContentText").HasColumnType("nvarchar(max)");
            entity.Property(x => x.Resolution).HasColumnName("Resolution").HasMaxLength(20);
            entity.Property(x => x.Status).HasColumnName("Status").HasMaxLength(50).HasDefaultValue("Active");
            entity.HasOne(x => x.AdCampaign)
                .WithMany(ac => ac.AdResources)
                .HasForeignKey(x => x.AdCampaignId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AdCampaignLog>(entity =>
        {
            entity.ToTable("AD_CAMPAIGN_LOG");
            entity.HasKey(x => x.LogId);
            entity.Property(x => x.LogId).HasColumnName("LogID");
            entity.Property(x => x.AdCampaignId).HasColumnName("AdCampaignID");
            entity.Property(x => x.ActionType).HasColumnName("ActionType").HasMaxLength(50).IsRequired();
            entity.Property(x => x.ChargedAmount).HasColumnName("ChargedAmount").HasPrecision(18, 2).HasDefaultValue(0m);
            entity.Property(x => x.Timestamp).HasColumnName("Timestamp").HasDefaultValueSql("DATEADD(hour, 7, GETUTCDATE())");

            // Phase B: RoutePass columns (nullable cho dữ liệu Click/View cũ)
            entity.Property(x => x.SponsoredId).HasColumnName("SponsoredID");
            entity.Property(x => x.ProductId).HasColumnName("ProductID");
            entity.Property(x => x.RobotId).HasColumnName("RobotID");
            entity.Property(x => x.RobotZoneId).HasColumnName("RobotZoneID");
            entity.Property(x => x.ZoneId).HasColumnName("ZoneID");
            entity.Property(x => x.SlotId).HasColumnName("SlotID");
            entity.Property(x => x.MemberId).HasColumnName("MemberID");
            entity.Property(x => x.SessionId).HasColumnName("SessionID").HasMaxLength(100);
            entity.Property(x => x.XCoord).HasColumnName("XCoord");
            entity.Property(x => x.YCoord).HasColumnName("YCoord");

            entity.HasOne(x => x.AdCampaign)
                .WithMany(ac => ac.AdCampaignLogs)
                .HasForeignKey(x => x.AdCampaignId)
                .OnDelete(DeleteBehavior.Cascade);
            // NoAction: chống cascade cycle — AD_CAMPAIGN_LOG là log table,
            // tất cả FK phụ để NoAction, tránh multiple cascade paths trên SQL Server
            entity.HasOne(x => x.SponsoredProduct)
                .WithMany()
                .HasForeignKey(x => x.SponsoredId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Robot)
                .WithMany()
                .HasForeignKey(x => x.RobotId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.RobotZone)
                .WithMany()
                .HasForeignKey(x => x.RobotZoneId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Zone)
                .WithMany()
                .HasForeignKey(x => x.ZoneId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Slot)
                .WithMany()
                .HasForeignKey(x => x.SlotId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Member)
                .WithMany()
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ─────────────────────────────────────────────
        // REGION 6: Robot & Navigation
        // ─────────────────────────────────────────────

        modelBuilder.Entity<Robot>(entity =>
        {
            entity.ToTable("ROBOT");
            entity.HasKey(x => x.RobotId);
            entity.Property(x => x.RobotId).HasColumnName("RobotID");
            entity.Property(x => x.RobotName).HasColumnName("RobotName").HasMaxLength(100).IsRequired();
            entity.Property(x => x.RobotCode).HasColumnName("RobotCode").HasMaxLength(50).IsRequired();
            entity.Property(x => x.BatteryPct).HasColumnName("BatteryPct").HasDefaultValue(100);
            entity.Property(x => x.Mode).HasColumnName("Mode").HasMaxLength(50).HasDefaultValue("idle");
            entity.Property(x => x.Status).HasColumnName("Status").HasMaxLength(50).IsRequired();
            entity.Property(x => x.LastSeenAt).HasColumnName("LastSeenAt");
            entity.HasIndex(x => x.RobotCode).IsUnique().HasDatabaseName("IX_ROBOT_RobotCode");
        });

        modelBuilder.Entity<RobotLog>(entity =>
        {
            entity.ToTable("ROBOT_LOG");
            entity.HasKey(x => x.LogId);
            entity.Property(x => x.LogId).HasColumnName("LogID");
            entity.Property(x => x.RobotId).HasColumnName("RobotID");
            entity.Property(x => x.Battery).HasColumnName("battery");
            entity.Property(x => x.Location).HasColumnName("location").HasMaxLength(200);
            entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
            entity.Property(x => x.Timestamp).HasColumnName("timestamp").HasDefaultValueSql("DATEADD(hour, 7, GETUTCDATE())");
            entity.Property(x => x.XCoord).HasColumnName("XCoord");
            entity.Property(x => x.YCoord).HasColumnName("YCoord");
            entity.Property(x => x.HeadingRad).HasColumnName("HeadingRad");
            entity.HasOne(x => x.Robot)
                .WithMany(r => r.RobotLogs)
                .HasForeignKey(x => x.RobotId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(x => new { x.RobotId, x.Timestamp }).HasDatabaseName("IX_ROBOT_LOG_robot_timestamp");
        });

        modelBuilder.Entity<RobotZone>(entity =>
        {
            entity.ToTable("ROBOT_ZONE");
            entity.HasKey(x => x.RobotZoneId);
            entity.Property(x => x.RobotZoneId).HasColumnName("RobotZoneID");
            entity.Property(x => x.RobotId).HasColumnName("RobotID");
            entity.Property(x => x.ZoneId).HasColumnName("ZoneID");
            entity.HasOne(x => x.Robot)
                .WithMany(r => r.RobotZones)
                .HasForeignKey(x => x.RobotId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Zone)
                .WithMany(z => z.RobotZones)
                .HasForeignKey(x => x.ZoneId);
        });

        modelBuilder.Entity<Map>(entity =>
        {
            entity.ToTable("MAP");
            entity.HasKey(x => x.MapId);
            entity.Property(x => x.MapId).HasColumnName("MapID");
            entity.Property(x => x.FloorId).HasColumnName("FloorID");
            entity.Property(x => x.MapName).HasColumnName("MapName").HasMaxLength(100).IsRequired();
            entity.Property(x => x.MapData).HasColumnName("MapData").HasColumnType("nvarchar(max)");
            entity.Property(x => x.FloorplanImageUrl).HasColumnName("FloorplanImageUrl").HasMaxLength(500);
            entity.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("DATEADD(hour, 7, GETUTCDATE())");
            entity.HasOne(x => x.Floor)
                .WithMany(f => f.Maps)
                .HasForeignKey(x => x.FloorId);
        });

        modelBuilder.Entity<NavigationNode>(entity =>
        {
            entity.ToTable("NAVIGATION_NODE");
            entity.HasKey(x => x.NodeId);
            entity.Property(x => x.NodeId).HasColumnName("NodeID");
            entity.Property(x => x.MapId).HasColumnName("MapID");
            entity.Property(x => x.NodeName).HasColumnName("NodeName").HasMaxLength(100);
            entity.Property(x => x.XCoord).HasColumnName("XCoord").HasDefaultValue(0);
            entity.Property(x => x.YCoord).HasColumnName("YCoord").HasDefaultValue(0);
            entity.Property(x => x.NodeType).HasColumnName("NodeType").HasMaxLength(50);
            entity.Property(x => x.IsBlocked).HasColumnName("IsBlocked").HasDefaultValue(false);
            entity.HasOne(x => x.Map)
                .WithMany(m => m.NavigationNodes)
                .HasForeignKey(x => x.MapId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NavigationEdge>(entity =>
        {
            entity.ToTable("NAVIGATION_EDGE");
            entity.HasKey(x => x.EdgeId);
            entity.Property(x => x.EdgeId).HasColumnName("EdgeID");
            entity.Property(x => x.FromNodeId).HasColumnName("FromNodeID");
            entity.Property(x => x.ToNodeId).HasColumnName("ToNodeID");
            entity.Property(x => x.Distance).HasColumnName("Distance").HasDefaultValue(0);
            entity.Property(x => x.IsBidirectional).HasColumnName("IsBidirectional").HasDefaultValue(true);
            // NoAction cả 2 chiều chống cascade cycle
            entity.HasOne(x => x.FromNode)
                .WithMany(n => n.OutgoingEdges)
                .HasForeignKey(x => x.FromNodeId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.ToNode)
                .WithMany(n => n.IncomingEdges)
                .HasForeignKey(x => x.ToNodeId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<AisleNode>(entity =>
        {
            entity.ToTable("AISLE_NODE");
            entity.HasKey(x => x.AisleNodeId);
            entity.Property(x => x.AisleNodeId).HasColumnName("AisleNodeID");
            entity.Property(x => x.AisleId).HasColumnName("AisleID");
            entity.Property(x => x.NodeId).HasColumnName("NodeID");
            entity.HasOne(x => x.Aisle)
                .WithMany(a => a.AisleNodes)
                .HasForeignKey(x => x.AisleId)
                .OnDelete(DeleteBehavior.Cascade);
            // NoAction: chống multiple cascade paths — AISLE_NODE đã có Cascade từ AISLE
            entity.HasOne(x => x.Node)
                .WithMany(n => n.AisleNodes)
                .HasForeignKey(x => x.NodeId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<RobotRoute>(entity =>
        {
            entity.ToTable("ROBOT_ROUTE");
            entity.HasKey(x => x.RobotRouteId);
            entity.Property(x => x.RobotRouteId).HasColumnName("RobotRouteID");
            entity.Property(x => x.RobotId).HasColumnName("RobotID");
            entity.Property(x => x.MapId).HasColumnName("MapID");
            entity.Property(x => x.RouteName).HasColumnName("RouteName").HasMaxLength(200);
            entity.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("DATEADD(hour, 7, GETUTCDATE())");
            entity.HasOne(x => x.Robot)
                .WithMany(r => r.RobotRoutes)
                .HasForeignKey(x => x.RobotId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Map)
                .WithMany(m => m.RobotRoutes)
                .HasForeignKey(x => x.MapId);
        });

        modelBuilder.Entity<RouteNodeMapping>(entity =>
        {
            entity.ToTable("ROUTE_NODE_MAPPING");
            entity.HasKey(x => x.RouteNodeMappingId);
            entity.Property(x => x.RouteNodeMappingId).HasColumnName("RouteNodeMappingID");
            entity.Property(x => x.RobotRouteId).HasColumnName("RobotRouteID");
            entity.Property(x => x.NodeId).HasColumnName("NodeID");
            entity.Property(x => x.SequenceOrder).HasColumnName("SequenceOrder").HasDefaultValue(0);
            entity.HasOne(x => x.RobotRoute)
                .WithMany(rr => rr.RouteNodeMappings)
                .HasForeignKey(x => x.RobotRouteId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Node)
                .WithMany(n => n.RouteNodeMappings)
                .HasForeignKey(x => x.NodeId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<RouteAssignment>(entity =>
        {
            entity.ToTable("ROUTE_ASSIGNMENT");
            entity.HasKey(x => x.RouteAssignmentId);
            entity.Property(x => x.RouteAssignmentId).HasColumnName("RouteAssignmentID");
            entity.Property(x => x.RobotId).HasColumnName("RobotID");
            entity.Property(x => x.RobotRouteId).HasColumnName("RobotRouteID");
            entity.Property(x => x.AssignedAt).HasColumnName("AssignedAt").HasDefaultValueSql("DATEADD(hour, 7, GETUTCDATE())");
            entity.Property(x => x.Status).HasColumnName("Status").HasMaxLength(50).IsRequired().HasDefaultValue("Pending");
            entity.HasOne(x => x.Robot)
                .WithMany(r => r.RouteAssignments)
                .HasForeignKey(x => x.RobotId)
                .OnDelete(DeleteBehavior.Cascade);
            // NoAction: chống cascade cycle (Robot→Route có thể tham chiếu vòng)
            entity.HasOne(x => x.RobotRoute)
                .WithMany(rr => rr.RouteAssignments)
                .HasForeignKey(x => x.RobotRouteId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<AisleScan>(entity =>
        {
            entity.ToTable("AISLE_SCAN");
            entity.HasKey(x => x.ScanId);
            entity.Property(x => x.ScanId).HasColumnName("ScanID");
            entity.Property(x => x.AisleId).HasColumnName("AisleID");
            entity.Property(x => x.AisleNodeId).HasColumnName("AisleNodeID");
            entity.Property(x => x.RobotId).HasColumnName("RobotID");
            entity.Property(x => x.ScannedAt).HasColumnName("ScannedAt").HasDefaultValueSql("DATEADD(hour, 7, GETUTCDATE())");
            entity.Property(x => x.EmptyPercentage).HasColumnName("EmptyPercentage").HasPrecision(5, 2).HasDefaultValue(0m);
            entity.Property(x => x.DensityPercentage).HasColumnName("DensityPercentage").HasPrecision(5, 2).HasDefaultValue(100m);
            entity.Property(x => x.NeedsRestock).HasColumnName("NeedsRestock").HasDefaultValue(false);
            entity.Property(x => x.ImageUrl).HasColumnName("ImageUrl").HasMaxLength(500);
            entity.HasOne(x => x.Aisle)
                .WithMany(a => a.AisleScans)
                .HasForeignKey(x => x.AisleId);
            // NoAction: chống multiple cascade paths — AISLE_SCAN đã có Cascade từ AISLE
            entity.HasOne(x => x.AisleNode)
                .WithMany()
                .HasForeignKey(x => x.AisleNodeId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Robot)
                .WithMany(r => r.AisleScans)
                .HasForeignKey(x => x.RobotId);
        });

        modelBuilder.Entity<SemanticObject>(entity =>
        {
            entity.ToTable("SEMANTIC_OBJECT");
            entity.HasKey(x => x.ObjectId);
            entity.Property(x => x.ObjectId).HasColumnName("ObjectID");
            entity.Property(x => x.MapId).HasColumnName("MapID");
            entity.Property(x => x.ObjectType).HasColumnName("ObjectType").HasMaxLength(100).IsRequired();
            entity.Property(x => x.XMin).HasColumnName("XMin").HasDefaultValue(0);
            entity.Property(x => x.YMin).HasColumnName("YMin").HasDefaultValue(0);
            entity.Property(x => x.XMax).HasColumnName("XMax").HasDefaultValue(0);
            entity.Property(x => x.YMax).HasColumnName("YMax").HasDefaultValue(0);
            entity.Property(x => x.Label).HasColumnName("Label").HasMaxLength(100);
            entity.Property(x => x.Confidence).HasColumnName("Confidence");
            entity.Property(x => x.DetectedAt).HasColumnName("DetectedAt");
            entity.Property(x => x.ImageUrl).HasColumnName("ImageUrl").HasMaxLength(500);
            entity.Property(x => x.ProductId).HasColumnName("ProductID");
            entity.HasOne(x => x.Map)
                .WithMany(m => m.SemanticObjects)
                .HasForeignKey(x => x.MapId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
