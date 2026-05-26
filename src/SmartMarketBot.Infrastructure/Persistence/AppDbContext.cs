using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Domain.Entities.Views;

namespace SmartMarketBot.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<Aisle> Aisles => Set<Aisle>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Floor> Floors => Set<Floor>();
    public DbSet<HealthTag> HealthTags => Set<HealthTag>();
    public DbSet<HistoryItem> HistoryItems => Set<HistoryItem>();
    public DbSet<Map> Maps => Set<Map>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<MemberHealthPreference> MemberHealthPreferences => Set<MemberHealthPreference>();
    public DbSet<NavigationEdge> NavigationEdges => Set<NavigationEdge>();
    public DbSet<NavigationNode> NavigationNodes => Set<NavigationNode>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductHealthTag> ProductHealthTags => Set<ProductHealthTag>();
    public DbSet<ProductType> ProductTypes => Set<ProductType>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<PromotionProduct> PromotionProducts => Set<PromotionProduct>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeItem> RecipeItems => Set<RecipeItem>();
    public DbSet<Robot> Robots => Set<Robot>();
    public DbSet<RobotLog> RobotLogs => Set<RobotLog>();
    public DbSet<RobotZone> RobotZones => Set<RobotZone>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<SemanticObject> SemanticObjects => Set<SemanticObject>();
    public DbSet<ShelfLevel> ShelfLevels => Set<ShelfLevel>();
    public DbSet<ShelfScan> ShelfScans => Set<ShelfScan>();
    public DbSet<ShoppingHistory> ShoppingHistories => Set<ShoppingHistory>();
    public DbSet<Slot> Slots => Set<Slot>();
    public DbSet<SponsoredProduct> SponsoredProducts => Set<SponsoredProduct>();
    public DbSet<Staff> Staffs => Set<Staff>();
    public DbSet<Subcategory> Subcategories => Set<Subcategory>();
    public DbSet<Supermarket> Supermarkets => Set<Supermarket>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Workstation> Workstations => Set<Workstation>();
    public DbSet<Zone> Zones => Set<Zone>();

    public DbSet<BlockedAisleView> BlockedAisleViews => Set<BlockedAisleView>();
    public DbSet<PurchaseHistoryView> PurchaseHistoryViews => Set<PurchaseHistoryView>();
    public DbSet<RealTimeStockView> RealTimeStockViews => Set<RealTimeStockView>();
    public DbSet<StoreMapView> StoreMapViews => Set<StoreMapView>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Admin>(entity =>
        {
            entity.ToTable("Admins");
            entity.HasKey(x => x.AdminID);
        });

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

        modelBuilder.Entity<Map>(entity =>
        {
            entity.ToTable("Maps");
            entity.HasKey(x => x.MapID);
        });

        modelBuilder.Entity<Member>(entity =>
        {
            entity.ToTable("Members");
            entity.HasKey(x => x.MemberID);
            entity.Property(x => x.MemberName)
                .HasComputedColumnSql("[FullName]", stored: false);
        });

        modelBuilder.Entity<MemberHealthPreference>(entity =>
        {
            entity.ToTable("MemberHealthPreferences");
            entity.HasKey(x => new { x.MemberID, x.TagID });
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

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(x => x.RoleID);
        });

        modelBuilder.Entity<SemanticObject>(entity =>
        {
            entity.ToTable("SemanticObjects");
            entity.HasKey(x => x.ObjectID);
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

        modelBuilder.Entity<SponsoredProduct>(entity =>
        {
            entity.ToTable("SponsoredProducts");
            entity.HasKey(x => x.SponsoredID);
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.ToTable("Staffs");
            entity.HasKey(x => x.StaffID);
        });

        modelBuilder.Entity<Subcategory>(entity =>
        {
            entity.ToTable("Subcategories");
            entity.HasKey(x => x.SubcategoryID);
        });

        modelBuilder.Entity<Supermarket>(entity =>
        {
            entity.ToTable("Supermarkets");
            entity.HasKey(x => x.SupermarketID);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(x => x.UserID);
            entity.HasIndex(x => x.Username).IsUnique();
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles");
            entity.HasKey(x => new { x.UserID, x.RoleID });
        });

        modelBuilder.Entity<Workstation>(entity =>
        {
            entity.ToTable("Workstations");
            entity.HasKey(x => x.WorkstationID);
        });

        modelBuilder.Entity<Zone>(entity =>
        {
            entity.ToTable("Zones");
            entity.HasKey(x => x.ZoneID);
        });

        modelBuilder.Entity<BlockedAisleView>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("BlockedAisleView");
        });

        modelBuilder.Entity<PurchaseHistoryView>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("PurchaseHistoryView");
        });

        modelBuilder.Entity<RealTimeStockView>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("RealTimeStockView");
        });

        modelBuilder.Entity<StoreMapView>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("StoreMapView");
        });
    }
}
