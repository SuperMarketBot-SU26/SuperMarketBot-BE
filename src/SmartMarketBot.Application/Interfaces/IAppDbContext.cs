using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Domain.Entities.Views;

namespace SmartMarketBot.Application.Interfaces;

public interface IAppDbContext
{
    // Region 1: Customer & Identity (5)
    DbSet<Account> Accounts { get; }
    DbSet<Member> Members { get; }
    DbSet<Membership> Memberships { get; }
    DbSet<HealthTag> HealthTags { get; }
    DbSet<MemberHealthPreference> MemberHealthPreferences { get; }
    DbSet<EmailOtp> EmailOtps { get; }
    DbSet<UserToken> UserTokens { get; }
    DbSet<MemberAlert> MemberAlerts { get; }
    DbSet<MemberEvent> MemberEvents { get; }

    // Region 2: Product Catalog
    DbSet<Category> Categories { get; }
    DbSet<Subcategory> Subcategories { get; }
    DbSet<ProductType> ProductTypes { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductHealthTag> ProductHealthTags { get; }

    // Region 3 (legacy): Ads & Promotions
    DbSet<Promotion> Promotions { get; }
    DbSet<PromotionProduct> PromotionProducts { get; }

    // Region 3: Shopping & Meal (4)
    DbSet<InvoiceHistory> InvoiceHistories { get; }
    DbSet<InvoiceHistoryItem> InvoiceHistoryItems { get; }
    DbSet<MealSuggestion> MealSuggestions { get; }
    DbSet<MealItem> MealItems { get; }

    // Region 4: Store Layout (6)
    DbSet<Floor> Floors { get; }
    DbSet<Zone> Zones { get; }
    DbSet<Aisle> Aisles { get; }
    DbSet<Shelf> Shelves { get; }
    DbSet<Slot> Slots { get; }
    DbSet<ProductSlot> ProductSlots { get; }

    // Region 5: Ad & Sponsorship (5)
    DbSet<Brand> Brands { get; }
    DbSet<AdPackage> AdPackages { get; }
    DbSet<AdCampaign> AdCampaigns { get; }
    DbSet<SponsoredProduct> SponsoredProducts { get; }
    DbSet<AdCampaignLog> AdCampaignLogs { get; }

    // Region 6: Robot & Navigation (10)
    DbSet<Robot> Robots { get; }
    DbSet<RobotLog> RobotLogs { get; }
    DbSet<RobotZone> RobotZones { get; }
    DbSet<Map> Maps { get; }
    DbSet<NavigationNode> NavigationNodes { get; }
    DbSet<NavigationEdge> NavigationEdges { get; }
    DbSet<AisleNode> AisleNodes { get; }
    DbSet<RobotRoute> RobotRoutes { get; }
    DbSet<RouteNodeMapping> RouteNodeMappings { get; }
    DbSet<RouteAssignment> RouteAssignments { get; }
    DbSet<AisleScan> AisleScans { get; }
    DbSet<SemanticObject> SemanticObjects { get; }

    // Views (keyless)
    DbSet<BlockedAisleView> BlockedAisleViews { get; }
    DbSet<PurchaseHistoryView> PurchaseHistoryViews { get; }
    DbSet<RealTimeStockView> RealTimeStockViews { get; }
    DbSet<StoreMapView> StoreMapViews { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
