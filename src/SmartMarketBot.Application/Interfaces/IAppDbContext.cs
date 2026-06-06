using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Domain.Entities.Views;

namespace SmartMarketBot.Application.Interfaces;

public interface IAppDbContext
{
    // Region 1: Customer & Identity
    DbSet<Account> Accounts { get; }
    DbSet<Admin> Admins { get; }
    DbSet<Member> Members { get; }
    DbSet<MemberHealthPreference> MemberHealthPreferences { get; }
    DbSet<MemberAlert> MemberAlerts { get; }
    DbSet<MemberEvent> MemberEvents { get; }
    DbSet<Staff> Staffs { get; }
    DbSet<UserToken> UserTokens { get; }
    DbSet<EmailOtp> EmailOtps { get; }

    // Region 2: Space & Goods
    DbSet<Aisle> Aisles { get; }
    DbSet<Category> Categories { get; }
    DbSet<Floor> Floors { get; }
    DbSet<HealthTag> HealthTags { get; }
    DbSet<HistoryItem> HistoryItems { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductHealthTag> ProductHealthTags { get; }
    DbSet<ProductType> ProductTypes { get; }
    DbSet<ShelfLevel> ShelfLevels { get; }
    DbSet<ShelfScan> ShelfScans { get; }
    DbSet<ShoppingHistory> ShoppingHistories { get; }
    DbSet<Slot> Slots { get; }
    DbSet<Subcategory> Subcategories { get; }
    DbSet<Zone> Zones { get; }

    // Region 3: Ads & Revenue
    DbSet<AdPackage> AdPackages { get; }
    DbSet<Brand> Brands { get; }
    DbSet<SponsoredProduct> SponsoredProducts { get; }
    DbSet<Promotion> Promotions { get; }
    DbSet<PromotionProduct> PromotionProducts { get; }
    DbSet<Recipe> Recipes { get; }
    DbSet<RecipeItem> RecipeItems { get; }

    // Region 4: Robot & Navigation
    DbSet<ForbiddenZone> ForbiddenZones { get; }
    DbSet<Map> Maps { get; }
    DbSet<NavigationEdge> NavigationEdges { get; }
    DbSet<NavigationNode> NavigationNodes { get; }
    DbSet<Robot> Robots { get; }
    DbSet<RobotLog> RobotLogs { get; }
    DbSet<RobotZone> RobotZones { get; }
    DbSet<SemanticObject> SemanticObjects { get; }
    DbSet<Workstation> Workstations { get; }

    // Views (keyless)
    DbSet<BlockedAisleView> BlockedAisleViews { get; }
    DbSet<PurchaseHistoryView> PurchaseHistoryViews { get; }
    DbSet<RealTimeStockView> RealTimeStockViews { get; }
    DbSet<StoreMapView> StoreMapViews { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
