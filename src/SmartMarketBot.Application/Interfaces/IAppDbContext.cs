using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Domain.Entities.Views;

namespace SmartMarketBot.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<Admin> Admins { get; }
    DbSet<Aisle> Aisles { get; }
    DbSet<Category> Categories { get; }
    DbSet<Floor> Floors { get; }
    DbSet<HealthTag> HealthTags { get; }
    DbSet<HistoryItem> HistoryItems { get; }
    DbSet<Map> Maps { get; }
    DbSet<Member> Members { get; }
    DbSet<MemberHealthPreference> MemberHealthPreferences { get; }
    DbSet<NavigationEdge> NavigationEdges { get; }
    DbSet<NavigationNode> NavigationNodes { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductHealthTag> ProductHealthTags { get; }
    DbSet<ProductType> ProductTypes { get; }
    DbSet<Promotion> Promotions { get; }
    DbSet<PromotionProduct> PromotionProducts { get; }
    DbSet<Recipe> Recipes { get; }
    DbSet<RecipeItem> RecipeItems { get; }
    DbSet<Robot> Robots { get; }
    DbSet<RobotLog> RobotLogs { get; }
    DbSet<RobotZone> RobotZones { get; }
    DbSet<Role> Roles { get; }
    DbSet<SemanticObject> SemanticObjects { get; }
    DbSet<ShelfLevel> ShelfLevels { get; }
    DbSet<ShelfScan> ShelfScans { get; }
    DbSet<ShoppingHistory> ShoppingHistories { get; }
    DbSet<Slot> Slots { get; }
    DbSet<SponsoredProduct> SponsoredProducts { get; }
    DbSet<Staff> Staffs { get; }
    DbSet<Subcategory> Subcategories { get; }
    DbSet<Supermarket> Supermarkets { get; }
    DbSet<User> Users { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<UserToken> UserTokens { get; }
    DbSet<EmailOtp> EmailOtps { get; }
    DbSet<Payment> Payments { get; }
    DbSet<Workstation> Workstations { get; }
    DbSet<Zone> Zones { get; }

    DbSet<BlockedAisleView> BlockedAisleViews { get; }
    DbSet<PurchaseHistoryView> PurchaseHistoryViews { get; }
    DbSet<RealTimeStockView> RealTimeStockViews { get; }
    DbSet<StoreMapView> StoreMapViews { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
