using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Domain.Entities.Views;

namespace SmartMarketBot.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    // ── Region 1: Customer & Identity ────────────────────────────────────────
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<MemberHealthPreference> MemberHealthPreferences => Set<MemberHealthPreference>();
    public DbSet<MemberAlert> MemberAlerts => Set<MemberAlert>();
    public DbSet<MemberEvent> MemberEvents => Set<MemberEvent>();
    public DbSet<Staff> Staffs => Set<Staff>();
    public DbSet<UserToken> UserTokens => Set<UserToken>();
    public DbSet<EmailOtp> EmailOtps => Set<EmailOtp>();

    // ── Region 2: Space & Goods ───────────────────────────────────────────────
    public DbSet<Aisle> Aisles => Set<Aisle>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Floor> Floors => Set<Floor>();
    public DbSet<HealthTag> HealthTags => Set<HealthTag>();
    public DbSet<HistoryItem> HistoryItems => Set<HistoryItem>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductHealthTag> ProductHealthTags => Set<ProductHealthTag>();
    public DbSet<ProductType> ProductTypes => Set<ProductType>();
    public DbSet<ShelfLevel> ShelfLevels => Set<ShelfLevel>();
    public DbSet<ShelfScan> ShelfScans => Set<ShelfScan>();
    public DbSet<ShoppingHistory> ShoppingHistories => Set<ShoppingHistory>();
    public DbSet<Slot> Slots => Set<Slot>();
    public DbSet<Subcategory> Subcategories => Set<Subcategory>();
    public DbSet<Zone> Zones => Set<Zone>();

    // ── Region 3: Ads & Revenue ───────────────────────────────────────────────
    public DbSet<AdPackage> AdPackages => Set<AdPackage>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<SponsoredProduct> SponsoredProducts => Set<SponsoredProduct>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<PromotionProduct> PromotionProducts => Set<PromotionProduct>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeItem> RecipeItems => Set<RecipeItem>();

    // ── Region 4: Robot & Navigation ─────────────────────────────────────────
    public DbSet<ForbiddenZone> ForbiddenZones => Set<ForbiddenZone>();
    public DbSet<Map> Maps => Set<Map>();
    public DbSet<NavigationEdge> NavigationEdges => Set<NavigationEdge>();
    public DbSet<NavigationNode> NavigationNodes => Set<NavigationNode>();
    public DbSet<Robot> Robots => Set<Robot>();
    public DbSet<RobotLog> RobotLogs => Set<RobotLog>();
    public DbSet<RobotZone> RobotZones => Set<RobotZone>();
    public DbSet<SemanticObject> SemanticObjects => Set<SemanticObject>();
    public DbSet<Workstation> Workstations => Set<Workstation>();

    // ── Views (keyless) ───────────────────────────────────────────────────────
    public DbSet<BlockedAisleView> BlockedAisleViews => Set<BlockedAisleView>();
    public DbSet<PurchaseHistoryView> PurchaseHistoryViews => Set<PurchaseHistoryView>();
    public DbSet<RealTimeStockView> RealTimeStockViews => Set<RealTimeStockView>();
    public DbSet<StoreMapView> StoreMapViews => Set<StoreMapView>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Region 1: Customer & Identity ────────────────────────────────────

        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("Accounts");
            entity.HasKey(x => x.AccountID);
            entity.HasIndex(x => x.Username).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.FullName).HasMaxLength(100);
            entity.Property(x => x.Phone).HasMaxLength(20);
            entity.Property(x => x.Role)
                .HasConversion<int>()
                .HasDefaultValue(AccountRole.Member);
        });

        modelBuilder.Entity<Admin>(entity =>
        {
            entity.ToTable("Admins");
            entity.HasKey(x => x.AdminID);
            entity.HasOne(x => x.Account).WithMany()
                .HasForeignKey(x => x.AccountID).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Member>(entity =>
        {
            entity.ToTable("Members");
            entity.HasKey(x => x.MemberID);
            entity.Property(x => x.MemberName)
                .HasComputedColumnSql("[FullName]", stored: false);
            entity.HasOne(x => x.Account).WithMany()
                .HasForeignKey(x => x.AccountID).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MemberHealthPreference>(entity =>
        {
            entity.ToTable("MemberHealthPreferences");
            entity.HasKey(x => new { x.MemberID, x.TagID });
        });

        modelBuilder.Entity<MemberAlert>(entity =>
        {
            entity.ToTable("MemberAlerts");
            entity.HasKey(x => x.AlertID);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(x => x.IsRead).HasDefaultValue(false);
            entity.HasIndex(x => new { x.MemberID, x.IsRead });
            entity.HasOne(x => x.Member).WithMany(m => m.MemberAlerts)
                .HasForeignKey(x => x.MemberID).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MemberEvent>(entity =>
        {
            entity.ToTable("MemberEvents");
            entity.HasKey(x => x.EventID);
            entity.Property(x => x.IsProcessed).HasDefaultValue(false);
            entity.HasIndex(x => new { x.EventDate, x.IsProcessed });
            entity.HasOne(x => x.Member).WithMany(m => m.MemberEvents)
                .HasForeignKey(x => x.MemberID).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.ToTable("Staff");
            entity.HasKey(x => x.StaffID);
            entity.HasOne(x => x.Account).WithMany()
                .HasForeignKey(x => x.AccountID).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserToken>(entity =>
        {
            entity.ToTable("UserTokens");
            entity.HasKey(x => x.TokenId);
            entity.Property(x => x.TokenId).HasDefaultValueSql("(newid())");
            entity.Property(x => x.RefreshToken).IsRequired().HasMaxLength(512);
            entity.Property(x => x.DeviceInfo).HasMaxLength(256);
            entity.Property(x => x.IpAddress).HasMaxLength(64);
            entity.Property(x => x.IsRevoked).HasDefaultValue(false);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.HasIndex(x => x.RefreshToken);
            entity.HasIndex(x => new { x.AccountId, x.IsRevoked });
            entity.HasOne(x => x.Account).WithMany(a => a.UserTokens)
                .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmailOtp>(entity =>
        {
            entity.ToTable("EmailOtps");
            entity.HasKey(x => x.OtpId);
            entity.Property(x => x.OtpId).HasDefaultValueSql("(newid())");
            entity.Property(x => x.Email).IsRequired().HasMaxLength(256);
            entity.Property(x => x.OtpCode).IsRequired().HasMaxLength(6);
            entity.Property(x => x.OtpType).IsRequired().HasMaxLength(50).HasDefaultValue("Registration");
            entity.Property(x => x.IsUsed).HasDefaultValue(false);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(x => x.TemporaryFullName).HasMaxLength(100);
            entity.Property(x => x.TemporaryPhone).HasMaxLength(20);
            entity.HasIndex(x => new { x.Email, x.OtpType, x.IsUsed })
                .HasFilter("[IsUsed] = 0");
            entity.HasIndex(x => x.ExpiredAt).HasFilter("[IsUsed] = 0");
        });

        // ── Region 2: Space & Goods ─────────────────────────────────────────

        modelBuilder.Entity<Aisle>(entity =>
        {
            entity.ToTable("Aisles");
            entity.HasKey(x => x.AisleID);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(x => x.CategoryID);
        });

        modelBuilder.Entity<Floor>(entity =>
        {
            entity.ToTable("Floors");
            entity.HasKey(x => x.FloorID);
        });

        modelBuilder.Entity<HealthTag>(entity =>
        {
            entity.ToTable("HealthTags");
            entity.HasKey(x => x.TagID);
        });

        modelBuilder.Entity<HistoryItem>(entity =>
        {
            entity.ToTable("HistoryItems");
            entity.HasKey(x => x.HistoryItemID);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(x => x.ProductID);
            entity.HasOne(x => x.SubstituteProduct)
                .WithMany()
                .HasForeignKey(x => x.SubstituteProductID)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<ProductHealthTag>(entity =>
        {
            entity.ToTable("ProductHealthTags");
            entity.HasKey(x => new { x.ProductID, x.TagID });
        });

        modelBuilder.Entity<ProductType>(entity =>
        {
            entity.ToTable("ProductTypes");
            entity.HasKey(x => x.ProductTypeID);
        });

        modelBuilder.Entity<ShelfLevel>(entity =>
        {
            entity.ToTable("ShelfLevels");
            entity.HasKey(x => x.ShelfLevelID);
        });

        modelBuilder.Entity<ShelfScan>(entity =>
        {
            entity.ToTable("ShelfScans");
            entity.HasKey(x => x.ScanID);
            entity.Property(x => x.NeedsRestock)
                .HasComputedColumnSql("CASE WHEN [EmptyPercentage] > 30 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END", stored: false);
        });

        modelBuilder.Entity<ShoppingHistory>(entity =>
        {
            entity.ToTable("ShoppingHistories");
            entity.HasKey(x => x.ShoppingHistoryID);
        });

        modelBuilder.Entity<Slot>(entity =>
        {
            entity.ToTable("Slots");
            entity.HasKey(x => x.SlotID);
        });

        modelBuilder.Entity<Subcategory>(entity =>
        {
            entity.ToTable("Subcategories");
            entity.HasKey(x => x.SubcategoryID);
        });

        modelBuilder.Entity<Zone>(entity =>
        {
            entity.ToTable("Zones");
            entity.HasKey(x => x.ZoneID);
        });

        // ── Region 3: Ads & Revenue ─────────────────────────────────────────

        modelBuilder.Entity<AdPackage>(entity =>
        {
            entity.ToTable("AdPackages");
            entity.HasKey(x => x.PackageID);
            entity.Property(x => x.Price).HasColumnType("decimal(18,2)");
            entity.Property(x => x.AdScore).HasDefaultValue(0);
            entity.Property(x => x.IsWeekendOnly).HasDefaultValue(false);
        });

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.ToTable("Brands");
            entity.HasKey(x => x.BrandID);
            entity.Property(x => x.BrandName).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<SponsoredProduct>(entity =>
        {
            entity.ToTable("SponsoredProducts");
            entity.HasKey(x => x.SponsoredID);
            entity.HasOne(x => x.Brand).WithMany(b => b.SponsoredProducts)
                .HasForeignKey(x => x.BrandID).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AdPackage).WithMany(ap => ap.SponsoredProducts)
                .HasForeignKey(x => x.PackageID).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Product).WithMany()
                .HasForeignKey(x => x.ProductID).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.ToTable("Promotions");
            entity.HasKey(x => x.PromotionID);
        });

        modelBuilder.Entity<PromotionProduct>(entity =>
        {
            entity.ToTable("PromotionProducts");
            entity.HasKey(x => new { x.PromotionID, x.ProductID });
        });

        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.ToTable("Recipes");
            entity.HasKey(x => x.RecipeID);
        });

        modelBuilder.Entity<RecipeItem>(entity =>
        {
            entity.ToTable("RecipeItems");
            entity.HasKey(x => new { x.RecipeID, x.ProductID });
        });

        // ── Region 4: Robot & Navigation ───────────────────────────────────

        modelBuilder.Entity<ForbiddenZone>(entity =>
        {
            entity.ToTable("ForbiddenZones");
            entity.HasKey(x => x.ForbiddenZoneID);
            entity.HasOne(x => x.Map).WithMany()
                .HasForeignKey(x => x.MapID).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Map>(entity =>
        {
            entity.ToTable("Maps");
            entity.HasKey(x => x.MapID);
        });

        modelBuilder.Entity<NavigationEdge>(entity =>
        {
            entity.ToTable("NavigationEdges");
            entity.HasKey(x => x.EdgeID);
            entity.HasOne(x => x.FromNode)
                .WithMany(x => x.OutgoingEdges)
                .HasForeignKey(x => x.FromNodeID)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.ToNode)
                .WithMany(x => x.IncomingEdges)
                .HasForeignKey(x => x.ToNodeID)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<NavigationNode>(entity =>
        {
            entity.ToTable("NavigationNodes");
            entity.HasKey(x => x.NodeID);
        });

        modelBuilder.Entity<Robot>(entity =>
        {
            entity.ToTable("Robots");
            entity.HasKey(x => x.RobotID);
            entity.HasIndex(x => x.RobotCode).IsUnique();
        });

        modelBuilder.Entity<RobotLog>(entity =>
        {
            entity.ToTable("Robot_Logs");
            entity.HasKey(x => x.LogID);
            entity.Property(x => x.battery).HasColumnName("battery");
            entity.Property(x => x.location).HasColumnName("location");
            entity.Property(x => x.status).HasColumnName("status");
            entity.Property(x => x.timestamp).HasColumnName("timestamp");
        });

        modelBuilder.Entity<RobotZone>(entity =>
        {
            entity.ToTable("RobotZones");
            entity.HasKey(x => new { x.RobotID, x.ZoneID });
        });

        modelBuilder.Entity<SemanticObject>(entity =>
        {
            entity.ToTable("SemanticObjects");
            entity.HasKey(x => x.ObjectID);
        });

        modelBuilder.Entity<Workstation>(entity =>
        {
            entity.ToTable("Workstations");
            entity.HasKey(x => x.WorkstationID);
        });

        // ── Views ──────────────────────────────────────────────────────────

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
