using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Domain.Entities.Views;

namespace SmartMarketBot.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    // ── Region 1: Customer & Identity ────────────────────────────────────────
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<HealthTag> HealthTags => Set<HealthTag>();
    public DbSet<MemberHealthPreference> MemberHealthPreferences => Set<MemberHealthPreference>();
    public DbSet<EmailOtp> EmailOtps => Set<EmailOtp>();
    public DbSet<UserToken> UserTokens => Set<UserToken>();
    public DbSet<MemberAlert> MemberAlerts => Set<MemberAlert>();
    public DbSet<MemberEvent> MemberEvents => Set<MemberEvent>();

    // ── Region 2: Product Catalog ────────────────────────────────────────────
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Subcategory> Subcategories => Set<Subcategory>();
    public DbSet<ProductType> ProductTypes => Set<ProductType>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductHealthTag> ProductHealthTags => Set<ProductHealthTag>();

    // Legacy (không thuộc ERD V4.0 cốt lõi - dùng cho service cũ)
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<PromotionProduct> PromotionProducts => Set<PromotionProduct>();

    // ── Region 3: Shopping & Meal ────────────────────────────────────────────
    public DbSet<InvoiceHistory> InvoiceHistories => Set<InvoiceHistory>();
    public DbSet<InvoiceHistoryItem> InvoiceHistoryItems => Set<InvoiceHistoryItem>();
    public DbSet<MealSuggestion> MealSuggestions => Set<MealSuggestion>();
    public DbSet<MealItem> MealItems => Set<MealItem>();

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

    // ── Views (keyless) ───────────────────────────────────────────────────────
    public DbSet<BlockedAisleView> BlockedAisleViews => Set<BlockedAisleView>();
    public DbSet<PurchaseHistoryView> PurchaseHistoryViews => Set<PurchaseHistoryView>();
    public DbSet<RealTimeStockView> RealTimeStockViews => Set<RealTimeStockView>();
    public DbSet<StoreMapView> StoreMapViews => Set<StoreMapView>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ─────────────────────────────────────────────
        // REGION 1: Customer & Identity
        // ─────────────────────────────────────────────

        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("ACCOUNT");
            entity.HasKey(x => x.AccountId);
            entity.Property(x => x.AccountId).HasColumnName("account_id");
            entity.Property(x => x.Username).HasColumnName("username").HasMaxLength(100).IsRequired();
            entity.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(500).IsRequired();
            entity.Property(x => x.Email).HasColumnName("email").HasMaxLength(256);
            entity.Property(x => x.EmailConfirmed).HasColumnName("email_confirmed").HasDefaultValue(false);
            entity.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(20);
            entity.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(100);
            entity.Property(x => x.AvatarUrl).HasColumnName("avatar_url").HasMaxLength(500);
            entity.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(x => x.Role).HasColumnName("role").HasMaxLength(20).HasDefaultValue("Member");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETUTCDATE()");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(x => x.Username).IsUnique().HasDatabaseName("IX_ACCOUNT_username");
            entity.HasIndex(x => x.Email)
                .IsUnique()
                .HasFilter("[email] IS NOT NULL")
                .HasDatabaseName("IX_ACCOUNT_email");
        });

        modelBuilder.Entity<Member>(entity =>
        {
            entity.ToTable("MEMBER");
            entity.HasKey(x => x.MemberId);
            entity.Property(x => x.MemberId).HasColumnName("member_id");
            entity.Property(x => x.AccountId).HasColumnName("account_id");
            entity.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(255).IsRequired();
            entity.Property(x => x.FacePath).HasColumnName("face_path").HasMaxLength(500);
            entity.Property(x => x.FaceVector).HasColumnName("face_vector").HasColumnType("nvarchar(max)");
            entity.Property(x => x.SpendingLimit).HasColumnName("spending_limit").HasPrecision(12, 2);
            entity.Property(x => x.WarningThreshold).HasColumnName("warning_threshold").HasPrecision(12, 2);
            entity.Property(x => x.TotalPoints).HasColumnName("total_points").HasDefaultValue(0);
            // Backward-compat columns (không thuộc ERD V4.0 - chỉ dùng cho code cũ)
            entity.Ignore(x => x.ShoppingBudget);
            entity.Ignore(x => x.SearchMode);
            entity.Ignore(x => x.Tier);
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
            entity.Property(x => x.MembershipId).HasColumnName("membership_id");
            entity.Property(x => x.MemberId).HasColumnName("member_id");
            entity.Property(x => x.TierName).HasColumnName("tier_name").HasMaxLength(20).IsRequired();
            entity.Property(x => x.PointsThreshold).HasColumnName("points_threshold");
            entity.HasOne(x => x.Member)
                .WithMany(m => m.Memberships)
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.MemberId, x.TierName }).IsUnique();
        });

        modelBuilder.Entity<HealthTag>(entity =>
        {
            entity.ToTable("HEALTH_TAG");
            entity.HasKey(x => x.HealthTagId);
            entity.Property(x => x.HealthTagId).HasColumnName("health_tag_id");
            entity.Property(x => x.TagName).HasColumnName("tag_name").HasMaxLength(50).IsRequired();
            entity.Property(x => x.TagType).HasColumnName("tag_type").HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<MemberHealthPreference>(entity =>
        {
            entity.ToTable("MEMBERHEALTH_PREFERENCE");
            entity.HasKey(x => new { x.MemberId, x.HealthTagId });
            entity.Property(x => x.MemberId).HasColumnName("member_id");
            entity.Property(x => x.HealthTagId).HasColumnName("health_tag_id");
            entity.Property(x => x.IsAllergy).HasColumnName("is_allergy").HasDefaultValue(false);
            entity.HasOne(x => x.Member)
                .WithMany(m => m.MemberHealthPreferences)
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.HealthTag)
                .WithMany(h => h.MemberHealthPreferences)
                .HasForeignKey(x => x.HealthTagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // EmailOtp - bảng phụ trợ cho AuthService (không thuộc ERD V4.0 cốt lõi)
        modelBuilder.Entity<EmailOtp>(entity =>
        {
            entity.ToTable("EMAIL_OTP");
            entity.HasKey(x => x.OtpId);
            entity.Property(x => x.OtpId).HasColumnName("otp_id").HasDefaultValueSql("NEWID()");
            entity.Property(x => x.Email).HasColumnName("email").HasMaxLength(256).IsRequired();
            entity.Property(x => x.OtpCode).HasColumnName("otp_code").HasMaxLength(6).IsRequired();
            entity.Property(x => x.OtpType).HasColumnName("otp_type").HasMaxLength(50).HasDefaultValue("Registration");
            entity.Property(x => x.IsUsed).HasColumnName("is_used").HasDefaultValue(false);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETUTCDATE()");
            entity.Property(x => x.ExpiredAt).HasColumnName("expired_at");
            entity.Property(x => x.TemporaryFullName).HasColumnName("temporary_full_name").HasMaxLength(100);
            entity.Property(x => x.TemporaryPhone).HasColumnName("temporary_phone").HasMaxLength(20);
            entity.Property(x => x.TemporaryPasswordHash).HasColumnName("temporary_password_hash").HasMaxLength(500);
        });

        // UserToken - refresh token
        modelBuilder.Entity<UserToken>(entity =>
        {
            entity.ToTable("USER_TOKEN");
            entity.HasKey(x => x.TokenId);
            entity.Property(x => x.TokenId).HasColumnName("token_id").HasDefaultValueSql("NEWID()");
            entity.Property(x => x.AccountId).HasColumnName("account_id");
            entity.Property(x => x.RefreshToken).HasColumnName("refresh_token").HasMaxLength(512).IsRequired();
            entity.Property(x => x.ExpiryDate).HasColumnName("expiry_date");
            entity.Property(x => x.IsRevoked).HasColumnName("is_revoked").HasDefaultValue(false);
            entity.Property(x => x.DeviceInfo).HasColumnName("device_info").HasMaxLength(256);
            entity.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(64);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETUTCDATE()");
            entity.HasOne(x => x.Account)
                .WithMany()                                   // không navigate ngược từ Account
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MemberAlert
        modelBuilder.Entity<MemberAlert>(entity =>
        {
            entity.ToTable("MEMBER_ALERT");
            entity.HasKey(x => x.AlertId);
            entity.Property(x => x.AlertId).HasColumnName("alert_id");
            entity.Property(x => x.MemberId).HasColumnName("member_id");
            entity.Property(x => x.AlertType).HasColumnName("alert_type").HasMaxLength(50);
            entity.Property(x => x.AlertMessage).HasColumnName("alert_message").HasMaxLength(1000);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETUTCDATE()");
            entity.Property(x => x.IsRead).HasColumnName("is_read").HasDefaultValue(false);
            entity.HasOne(x => x.Member)
                .WithMany()
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MemberEvent
        modelBuilder.Entity<MemberEvent>(entity =>
        {
            entity.ToTable("MEMBER_EVENT");
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.EventId).HasColumnName("event_id");
            entity.Property(x => x.MemberId).HasColumnName("member_id");
            entity.Property(x => x.EventName).HasColumnName("event_name").HasMaxLength(50);
            entity.Property(x => x.EventDate).HasColumnName("event_date");
            entity.Property(x => x.DiscountPct).HasColumnName("discount_pct").HasPrecision(18, 2);
            entity.Property(x => x.IsProcessed).HasColumnName("is_processed").HasDefaultValue(false);
        });

        // Promotion (legacy - không thuộc ERD V4.0)
        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.ToTable("PROMOTION");
            entity.HasKey(x => x.PromotionId);
            entity.Property(x => x.PromotionId).HasColumnName("promotion_id");
            entity.Property(x => x.PromotionName).HasColumnName("promotion_name").HasMaxLength(200);
            entity.Property(x => x.Description).HasColumnName("description");
            entity.Property(x => x.DiscountValue).HasColumnName("discount_value").HasPrecision(18, 2);
            entity.Property(x => x.StartDate).HasColumnName("start_date");
            entity.Property(x => x.EndDate).HasColumnName("end_date");
            entity.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        });

        // PromotionProduct (legacy)
        modelBuilder.Entity<PromotionProduct>(entity =>
        {
            entity.ToTable("PROMOTION_PRODUCT");
            entity.HasKey(x => new { x.PromotionId, x.ProductId });
            entity.Property(x => x.PromotionId).HasColumnName("promotion_id");
            entity.Property(x => x.ProductId).HasColumnName("product_id");
            entity.Property(x => x.Priority).HasColumnName("priority").HasDefaultValue(0);
        });

        // ─────────────────────────────────────────────
        // REGION 2: Product Catalog
        // ─────────────────────────────────────────────

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("CATEGORY");
            entity.HasKey(x => x.CategoryId);
            entity.Property(x => x.CategoryId).HasColumnName("category_id");
            entity.Property(x => x.CategoryName).HasColumnName("category_name").HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
        });

        modelBuilder.Entity<Subcategory>(entity =>
        {
            entity.ToTable("SUBCATEGORY");
            entity.HasKey(x => x.SubcategoryId);
            entity.Property(x => x.SubcategoryId).HasColumnName("subcategory_id");
            entity.Property(x => x.CategoryId).HasColumnName("category_id");
            entity.Property(x => x.SubcategoryName).HasColumnName("subcategory_name").HasMaxLength(100).IsRequired();
            entity.HasOne(x => x.Category)
                .WithMany(c => c.Subcategories)
                .HasForeignKey(x => x.CategoryId);
        });

        modelBuilder.Entity<ProductType>(entity =>
        {
            entity.ToTable("PRODUCT_TYPE");
            entity.HasKey(x => x.ProductTypeId);
            entity.Property(x => x.ProductTypeId).HasColumnName("product_type_id");
            entity.Property(x => x.SubcategoryId).HasColumnName("subcategory_id");
            entity.Property(x => x.TypeName).HasColumnName("type_name").HasMaxLength(100).IsRequired();
            entity.HasOne(x => x.Subcategory)
                .WithMany(s => s.ProductTypes)
                .HasForeignKey(x => x.SubcategoryId);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("PRODUCT");
            entity.HasKey(x => x.ProductId);
            entity.Property(x => x.ProductId).HasColumnName("product_id");
            entity.Property(x => x.ProductTypeId).HasColumnName("product_type_id");
            entity.Property(x => x.ProductName).HasColumnName("product_name").HasMaxLength(200).IsRequired();
            entity.Property(x => x.UnitPrice).HasColumnName("unit_price").HasPrecision(12, 2);
            entity.Property(x => x.Barcode).HasColumnName("barcode").HasMaxLength(50);
            entity.Property(x => x.ImageUrl).HasColumnName("image_url").HasMaxLength(500);
            entity.Property(x => x.WeightOrVolume).HasColumnName("weight_or_volume").HasPrecision(10, 2);
            entity.Property(x => x.Unit).HasColumnName("unit").HasMaxLength(20);
            entity.Property(x => x.Description).HasColumnName("description").HasMaxLength(1000);
            entity.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(x => x.SubstituteProductId).HasColumnName("substitute_product_id");
            entity.HasOne(x => x.ProductType)
                .WithMany(pt => pt.Products)
                .HasForeignKey(x => x.ProductTypeId);
            // Self-ref: substitute product — NoAction to prevent cascade cycle
            entity.HasOne(x => x.SubstituteProduct)
                .WithMany()
                .HasForeignKey(x => x.SubstituteProductId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasIndex(x => x.Barcode)
                .IsUnique()
                .HasFilter("[barcode] IS NOT NULL")
                .HasDatabaseName("IX_PRODUCT_barcode");
        });

        modelBuilder.Entity<ProductHealthTag>(entity =>
        {
            entity.ToTable("PRODUCT_HEALTHTAG");
            entity.HasKey(x => new { x.ProductId, x.HealthTagId });
            entity.Property(x => x.ProductId).HasColumnName("product_id");
            entity.Property(x => x.HealthTagId).HasColumnName("health_tag_id");
            entity.HasOne(x => x.Product)
                .WithMany(p => p.ProductHealthTags)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.HealthTag)
                .WithMany(h => h.ProductHealthTags)
                .HasForeignKey(x => x.HealthTagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ─────────────────────────────────────────────
        // REGION 3: Shopping & Meal
        // ─────────────────────────────────────────────

        modelBuilder.Entity<InvoiceHistory>(entity =>
        {
            entity.ToTable("INVOICE_HISTORY");
            entity.HasKey(x => x.InvoiceHistoryId);
            entity.Property(x => x.InvoiceHistoryId).HasColumnName("invoice_history_id");
            entity.Property(x => x.MemberId).HasColumnName("member_id");
            entity.Property(x => x.PurchaseDate).HasColumnName("purchase_date").HasDefaultValueSql("GETUTCDATE()");
            entity.Property(x => x.TotalAmount).HasColumnName("total_amount").HasPrecision(12, 2);
            entity.HasOne(x => x.Member)
                .WithMany(m => m.InvoiceHistories)
                .HasForeignKey(x => x.MemberId);
        });

        modelBuilder.Entity<InvoiceHistoryItem>(entity =>
        {
            entity.ToTable("INVOICE_HISTORY_ITEM");
            entity.HasKey(x => x.InvoiceHistoryItemId);
            entity.Property(x => x.InvoiceHistoryItemId).HasColumnName("invoice_history_item_id");
            entity.Property(x => x.InvoiceHistoryId).HasColumnName("invoice_history_id");
            entity.Property(x => x.ProductId).HasColumnName("product_id");
            entity.Property(x => x.Quantity).HasColumnName("quantity").HasDefaultValue(1);
            entity.Property(x => x.UnitPrice).HasColumnName("unit_price").HasPrecision(12, 2);
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
            entity.Property(x => x.MealSuggestionId).HasColumnName("meal_suggestion_id");
            entity.Property(x => x.MealName).HasColumnName("meal_name").HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasColumnName("description").HasColumnType("nvarchar(max)");
            entity.Property(x => x.YieldPortions).HasColumnName("yield_portions").HasDefaultValue(1);
            entity.Property(x => x.ImageUrl).HasColumnName("image_url").HasMaxLength(500);
            entity.Property(x => x.Calories).HasColumnName("calories");
            entity.Property(x => x.HealthyScore).HasColumnName("healthy_score");
            entity.Property(x => x.AlternativeSuggestion).HasColumnName("alternative_suggestion").HasMaxLength(500);
        });

        modelBuilder.Entity<MealItem>(entity =>
        {
            entity.ToTable("MEAL_ITEM");
            entity.HasKey(x => new { x.MealSuggestionId, x.ProductId });
            entity.Property(x => x.MealSuggestionId).HasColumnName("meal_suggestion_id");
            entity.Property(x => x.ProductId).HasColumnName("product_id");
            entity.Property(x => x.QuantityRequired).HasColumnName("quantity_required").HasPrecision(10, 2);
            entity.Property(x => x.UnitOfMeasure).HasColumnName("unit_of_measure").HasMaxLength(20);
            entity.HasOne(x => x.MealSuggestion)
                .WithMany(ms => ms.MealItems)
                .HasForeignKey(x => x.MealSuggestionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Product)
                .WithMany(p => p.MealItems)
                .HasForeignKey(x => x.ProductId);
        });

        // ─────────────────────────────────────────────
        // REGION 4: Store Layout
        // ─────────────────────────────────────────────

        modelBuilder.Entity<Floor>(entity =>
        {
            entity.ToTable("FLOOR");
            entity.HasKey(x => x.FloorId);
            entity.Property(x => x.FloorId).HasColumnName("floor_id");
            entity.Property(x => x.FloorNumber).HasColumnName("floor_number");
        });

        modelBuilder.Entity<Zone>(entity =>
        {
            entity.ToTable("ZONE");
            entity.HasKey(x => x.ZoneId);
            entity.Property(x => x.ZoneId).HasColumnName("zone_id");
            entity.Property(x => x.FloorId).HasColumnName("floor_id");
            entity.Property(x => x.ZoneCode).HasColumnName("zone_code").HasColumnType("char(1)").IsRequired();
            entity.Property(x => x.ZoneName).HasColumnName("zone_name").HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
            entity.Property(x => x.IsBlocked).HasColumnName("is_blocked").HasDefaultValue(false);
            entity.HasOne(x => x.Floor)
                .WithMany(f => f.Zones)
                .HasForeignKey(x => x.FloorId);
        });

        modelBuilder.Entity<Aisle>(entity =>
        {
            entity.ToTable("AISLE");
            entity.HasKey(x => x.AisleId);
            entity.Property(x => x.AisleId).HasColumnName("aisle_id");
            entity.Property(x => x.ZoneId).HasColumnName("zone_id");
            entity.Property(x => x.AisleCode).HasColumnName("aisle_code").HasMaxLength(10).IsRequired();
            entity.Property(x => x.AisleName).HasColumnName("aisle_name").HasMaxLength(100);
            entity.Property(x => x.IsBlocked).HasColumnName("is_blocked").HasDefaultValue(false);
            entity.HasOne(x => x.Zone)
                .WithMany(z => z.Aisles)
                .HasForeignKey(x => x.ZoneId);
        });

        modelBuilder.Entity<Shelf>(entity =>
        {
            entity.ToTable("SHELF");
            entity.HasKey(x => x.ShelfId);
            entity.Property(x => x.ShelfId).HasColumnName("shelf_id");
            entity.Property(x => x.AisleId).HasColumnName("aisle_id");
            entity.Property(x => x.LevelNumber).HasColumnName("level_number");
            entity.HasOne(x => x.Aisle)
                .WithMany(a => a.Shelves)
                .HasForeignKey(x => x.AisleId);
        });

        modelBuilder.Entity<Slot>(entity =>
        {
            entity.ToTable("SLOT");
            entity.HasKey(x => x.SlotId);
            entity.Property(x => x.SlotId).HasColumnName("slot_id");
            entity.Property(x => x.ShelfId).HasColumnName("shelf_id");
            entity.Property(x => x.SlotCode).HasColumnName("slot_code").HasMaxLength(10).IsRequired();
            entity.Property(x => x.Quantity).HasColumnName("quantity").HasDefaultValue(0);
            entity.Property(x => x.ExpiryDate).HasColumnName("expiry_date").HasColumnType("date");
            entity.Property(x => x.Supplier).HasColumnName("supplier").HasMaxLength(200);
            entity.Property(x => x.LastScannedAt).HasColumnName("last_scanned_at");
            entity.HasOne(x => x.Shelf)
                .WithMany(s => s.Slots)
                .HasForeignKey(x => x.ShelfId);
        });

        modelBuilder.Entity<ProductSlot>(entity =>
        {
            entity.ToTable("PRODUCT_SLOT");
            entity.HasKey(x => x.ProductSlotId);
            entity.Property(x => x.ProductSlotId).HasColumnName("product_slot_id");
            entity.Property(x => x.SlotId).HasColumnName("slot_id");
            entity.Property(x => x.ProductId).HasColumnName("product_id");
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
            entity.Property(x => x.BrandId).HasColumnName("brand_id");
            entity.Property(x => x.BrandName).HasColumnName("brand_name").HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
        });

        modelBuilder.Entity<AdPackage>(entity =>
        {
            entity.ToTable("AD_PACKAGE");
            entity.HasKey(x => x.PackageId);
            entity.Property(x => x.PackageId).HasColumnName("package_id");
            entity.Property(x => x.PackageName).HasColumnName("package_name").HasMaxLength(100).IsRequired();
            entity.Property(x => x.Price).HasColumnName("price").HasPrecision(18, 2);
            entity.Property(x => x.AdScore).HasColumnName("ad_score").HasDefaultValue(0);
            entity.Property(x => x.IsWeekendOnly).HasColumnName("is_weekend_only").HasDefaultValue(false);
        });

        modelBuilder.Entity<AdCampaign>(entity =>
        {
            entity.ToTable("AD_CAMPAIGN");
            entity.HasKey(x => x.AdCampaignId);
            entity.Property(x => x.AdCampaignId).HasColumnName("ad_campaign_id");
            entity.Property(x => x.PackageId).HasColumnName("package_id");
            entity.Property(x => x.BrandId).HasColumnName("brand_id");
            entity.Property(x => x.CampaignName).HasColumnName("campaign_name").HasMaxLength(200).IsRequired();
            entity.Property(x => x.StartDate).HasColumnName("start_date");
            entity.Property(x => x.EndDate).HasColumnName("end_date");
            entity.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            // NoAction: chống cascade cycle
            entity.HasOne(x => x.Package)
                .WithMany(p => p.AdCampaigns)
                .HasForeignKey(x => x.PackageId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Brand)
                .WithMany(b => b.AdCampaigns)
                .HasForeignKey(x => x.BrandId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<SponsoredProduct>(entity =>
        {
            entity.ToTable("SPONSORED_PRODUCT");
            entity.HasKey(x => x.SponsoredId);
            entity.Property(x => x.SponsoredId).HasColumnName("sponsored_id");
            entity.Property(x => x.AdCampaignId).HasColumnName("ad_campaign_id");
            entity.Property(x => x.ProductId).HasColumnName("product_id");
            entity.Property(x => x.BrandId).HasColumnName("brand_id");
            entity.Property(x => x.Priority).HasColumnName("priority").HasDefaultValue(0);
            entity.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.HasOne(x => x.AdCampaign)
                .WithMany(ac => ac.SponsoredProducts)
                .HasForeignKey(x => x.AdCampaignId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Product)
                .WithMany(p => p.SponsoredProducts)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            // Brand: NoAction để bảo vệ brand
            entity.HasOne(x => x.Brand)
                .WithMany(b => b.SponsoredProducts)
                .HasForeignKey(x => x.BrandId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<AdCampaignLog>(entity =>
        {
            entity.ToTable("AD_CAMPAIGN_LOG");
            entity.HasKey(x => x.LogId);
            entity.Property(x => x.LogId).HasColumnName("log_id");
            entity.Property(x => x.AdCampaignId).HasColumnName("ad_campaign_id");
            entity.Property(x => x.ActionType).HasColumnName("action_type").HasMaxLength(50).IsRequired();
            entity.Property(x => x.Timestamp).HasColumnName("timestamp").HasDefaultValueSql("GETUTCDATE()");
            entity.HasOne(x => x.AdCampaign)
                .WithMany(ac => ac.AdCampaignLogs)
                .HasForeignKey(x => x.AdCampaignId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ─────────────────────────────────────────────
        // REGION 6: Robot & Navigation
        // ─────────────────────────────────────────────

        modelBuilder.Entity<Robot>(entity =>
        {
            entity.ToTable("ROBOT");
            entity.HasKey(x => x.RobotId);
            entity.Property(x => x.RobotId).HasColumnName("robot_id");
            entity.Property(x => x.RobotName).HasColumnName("robot_name").HasMaxLength(100).IsRequired();
            entity.Property(x => x.RobotCode).HasColumnName("robot_code").HasMaxLength(50).IsRequired();
            entity.Property(x => x.BatteryPct).HasColumnName("battery_pct").HasDefaultValue(100);
            entity.Property(x => x.Mode).HasColumnName("mode").HasMaxLength(20).HasDefaultValue("idle");
            entity.Property(x => x.IsOnline).HasColumnName("is_online").HasDefaultValue(false);
            entity.Property(x => x.LastSeenAt).HasColumnName("last_seen_at");
            entity.HasIndex(x => x.RobotCode).IsUnique().HasDatabaseName("IX_ROBOT_robot_code");
        });

        modelBuilder.Entity<RobotLog>(entity =>
        {
            entity.ToTable("ROBOT_LOG");
            entity.HasKey(x => x.LogId);
            entity.Property(x => x.LogId).HasColumnName("log_id");
            entity.Property(x => x.RobotId).HasColumnName("robot_id");
            entity.Property(x => x.Battery).HasColumnName("battery");
            entity.Property(x => x.Location).HasColumnName("location").HasMaxLength(255);
            entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(100);
            entity.Property(x => x.Timestamp).HasColumnName("timestamp").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.XCoord).HasColumnName("x_coord");
            entity.Property(x => x.YCoord).HasColumnName("y_coord");
            entity.Property(x => x.HeadingRad).HasColumnName("heading_rad");
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
            entity.Property(x => x.RobotZoneId).HasColumnName("robot_zone_id");
            entity.Property(x => x.RobotId).HasColumnName("robot_id");
            entity.Property(x => x.ZoneId).HasColumnName("zone_id");
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
            entity.Property(x => x.MapId).HasColumnName("map_id");
            entity.Property(x => x.FloorId).HasColumnName("floor_id");
            entity.Property(x => x.MapName).HasColumnName("map_name").HasMaxLength(100).IsRequired();
            entity.Property(x => x.MapData).HasColumnName("map_data").HasColumnType("nvarchar(max)");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETUTCDATE()");
            entity.HasOne(x => x.Floor)
                .WithMany(f => f.Maps)
                .HasForeignKey(x => x.FloorId);
        });

        modelBuilder.Entity<NavigationNode>(entity =>
        {
            entity.ToTable("NAVIGATION_NODE");
            entity.HasKey(x => x.NodeId);
            entity.Property(x => x.NodeId).HasColumnName("node_id");
            entity.Property(x => x.MapId).HasColumnName("map_id");
            entity.Property(x => x.NodeName).HasColumnName("node_name").HasMaxLength(100).IsRequired();
            entity.Property(x => x.XCoord).HasColumnName("x_coord");
            entity.Property(x => x.YCoord).HasColumnName("y_coord");
            entity.Property(x => x.NodeType).HasColumnName("node_type").HasMaxLength(20).IsRequired();
            entity.Property(x => x.IsBlocked).HasColumnName("is_blocked").HasDefaultValue(false);
            entity.HasOne(x => x.Map)
                .WithMany(m => m.NavigationNodes)
                .HasForeignKey(x => x.MapId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NavigationEdge>(entity =>
        {
            entity.ToTable("NAVIGATION_EDGE");
            entity.HasKey(x => x.EdgeId);
            entity.Property(x => x.EdgeId).HasColumnName("edge_id");
            entity.Property(x => x.FromNodeId).HasColumnName("from_node_id");
            entity.Property(x => x.ToNodeId).HasColumnName("to_node_id");
            entity.Property(x => x.Distance).HasColumnName("distance");
            entity.Property(x => x.IsBidirectional).HasColumnName("is_bidirectional").HasDefaultValue(true);
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
            entity.Property(x => x.AisleNodeId).HasColumnName("aisle_node_id");
            entity.Property(x => x.AisleId).HasColumnName("aisle_id");
            entity.Property(x => x.NodeId).HasColumnName("node_id");
            entity.HasOne(x => x.Aisle)
                .WithMany(a => a.AisleNodes)
                .HasForeignKey(x => x.AisleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Node)
                .WithMany(n => n.AisleNodes)
                .HasForeignKey(x => x.NodeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RobotRoute>(entity =>
        {
            entity.ToTable("ROBOT_ROUTE");
            entity.HasKey(x => x.RobotRouteId);
            entity.Property(x => x.RobotRouteId).HasColumnName("robot_route_id");
            entity.Property(x => x.RobotId).HasColumnName("robot_id");
            entity.Property(x => x.MapId).HasColumnName("map_id");
            entity.Property(x => x.RouteName).HasColumnName("route_name").HasMaxLength(200).IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETUTCDATE()");
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
            entity.Property(x => x.RouteNodeMappingId).HasColumnName("route_node_mapping_id");
            entity.Property(x => x.RobotRouteId).HasColumnName("robot_route_id");
            entity.Property(x => x.NodeId).HasColumnName("node_id");
            entity.Property(x => x.SequenceOrder).HasColumnName("sequence_order");
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
            entity.Property(x => x.RouteAssignmentId).HasColumnName("route_assignment_id");
            entity.Property(x => x.RobotId).HasColumnName("robot_id");
            entity.Property(x => x.RobotRouteId).HasColumnName("robot_route_id");
            entity.Property(x => x.AssignedAt).HasColumnName("assigned_at").HasDefaultValueSql("GETUTCDATE()");
            entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
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
            entity.Property(x => x.ScanId).HasColumnName("scan_id");
            entity.Property(x => x.AisleId).HasColumnName("aisle_id");
            entity.Property(x => x.RobotId).HasColumnName("robot_id");
            entity.Property(x => x.ScannedAt).HasColumnName("scanned_at").HasDefaultValueSql("GETUTCDATE()");
            entity.Property(x => x.ImageUrl).HasColumnName("image_url").HasMaxLength(500);
            entity.Property(x => x.EmptyPercentage).HasColumnName("empty_percentage").HasPrecision(5, 2);
            // Computed column: AS CASE WHEN empty_percentage > 30 THEN 1 ELSE 0
            entity.Property(x => x.NeedsRestock)
                .HasColumnName("needs_restock")
                .HasComputedColumnSql("CASE WHEN [empty_percentage] > 30.0 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END", stored: true);
            entity.Property(x => x.AiResponseRaw).HasColumnName("ai_response_raw").HasColumnType("nvarchar(max)");
            entity.HasOne(x => x.Aisle)
                .WithMany(a => a.AisleScans)
                .HasForeignKey(x => x.AisleId);
            entity.HasOne(x => x.Robot)
                .WithMany(r => r.AisleScans)
                .HasForeignKey(x => x.RobotId);
        });

        modelBuilder.Entity<SemanticObject>(entity =>
        {
            entity.ToTable("SEMANTIC_OBJECT");
            entity.HasKey(x => x.ObjectId);
            entity.Property(x => x.ObjectId).HasColumnName("object_id");
            entity.Property(x => x.MapId).HasColumnName("map_id");
            entity.Property(x => x.ObjectType).HasColumnName("object_type").HasMaxLength(50).IsRequired();
            entity.Property(x => x.XMin).HasColumnName("x_min");
            entity.Property(x => x.YMin).HasColumnName("y_min");
            entity.Property(x => x.XMax).HasColumnName("x_max");
            entity.Property(x => x.YMax).HasColumnName("y_max");
            entity.Property(x => x.Label).HasColumnName("label").HasMaxLength(100);
            entity.Property(x => x.Confidence).HasColumnName("confidence").HasPrecision(5, 2);
            entity.Property(x => x.DetectedAt).HasColumnName("detected_at");
            entity.Property(x => x.ImageUrl).HasColumnName("image_url").HasMaxLength(500);
            entity.HasOne(x => x.Map)
                .WithMany(m => m.SemanticObjects)
                .HasForeignKey(x => x.MapId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ─────────────────────────────────────────────
        // VIEWS (keyless)
        // ─────────────────────────────────────────────

        modelBuilder.Entity<BlockedAisleView>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("Blocked_Aisles");
        });

        modelBuilder.Entity<PurchaseHistoryView>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("PurchaseHistory");
        });

        modelBuilder.Entity<RealTimeStockView>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("Real_Time_Stock");
        });

        modelBuilder.Entity<StoreMapView>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("Store_Map");
        });
    }
}
