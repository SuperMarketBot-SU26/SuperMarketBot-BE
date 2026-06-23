// ============================================================================
// SmartMarketBot — Domain Entity Models (ERD V4.0)
// Những entity này map 1-1 với bảng trong db/ddl_erd.sql
// Nằm trong src/SmartMarketBot.Domain/Entities/
// ============================================================================

namespace SmartMarketBot.Domain.Entities;

// ════════════════════════════════════════════════════════════════════════════
// REGION 1: Customer & Identity
// ════════════════════════════════════════════════════════════════════════════

/// <summary>Bảng tài khoản — gộp cả OTP, RefreshToken.</summary>
public class Account
{
    public int AccountId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? FullName { get; set; }
    public string Status { get; set; } = "Active";
    public string Role { get; set; } = "Member";
    public string? OtpCode { get; set; }
    public DateTime? OtpExpiredAt { get; set; }
    public string? OtpType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshExpiry { get; set; }
    public bool IsTokenRevoked { get; set; } = false;

    public Member? Member { get; set; }
}

/// <summary>Thông tin hội viên — liên kết 1-1 với Account.</summary>
public class Member
{
    public int MemberId { get; set; }
    public int? AccountId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? FacePath { get; set; }
    public string? FaceVector { get; set; }
    public decimal? SpendingLimit { get; set; }
    public int TotalPoints { get; set; } = 0;

    public Account? Account { get; set; }
    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    public ICollection<MemberHealthPreference> MemberHealthPreferences { get; set; } = new List<MemberHealthPreference>();
    public ICollection<InvoiceHistory> InvoiceHistories { get; set; } = new List<InvoiceHistory>();
}

/// <summary>Hạng thẻ thành viên: Bronze | Silver | Gold | Platinum.</summary>
public class Membership
{
    public int MembershipId { get; set; }
    public int MemberId { get; set; }
    public string TierName { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }

    public Member? Member { get; set; }
}

/// <summary>Tag sức khỏe: diet | allergy | ingredient | lifestyle.</summary>
public class HealthTag
{
    public int HealthTagId { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string TagType { get; set; } = "diet";

    public ICollection<MemberHealthPreference> MemberHealthPreferences { get; set; } = new List<MemberHealthPreference>();
    public ICollection<ProductHealthTag> ProductHealthTags { get; set; } = new List<ProductHealthTag>();
}

/// <summary>Chế độ ăn / dị ứng của member với 1 tag: Allergy | Avoid | Preferred.</summary>
public class MemberHealthPreference
{
    public int MemberId { get; set; }
    public int HealthTagId { get; set; }
    public string Status { get; set; } = string.Empty;  // Allergy | Avoid | Preferred

    public Member? Member { get; set; }
    public HealthTag? HealthTag { get; set; }
}

// ════════════════════════════════════════════════════════════════════════════
// REGION 2: Product Catalog
// ════════════════════════════════════════════════════════════════════════════

public class Category
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? CategoryIcon { get; set; }

    public ICollection<Subcategory> Subcategories { get; set; } = new List<Subcategory>();
}

public class Subcategory
{
    public int SubcategoryId { get; set; }
    public int CategoryId { get; set; }
    public string SubcategoryName { get; set; } = string.Empty;

    public Category? Category { get; set; }
    public ICollection<ProductType> ProductTypes { get; set; } = new List<ProductType>();
}

public class ProductType
{
    public int ProductTypeId { get; set; }
    public int SubcategoryId { get; set; }
    public string TypeName { get; set; } = string.Empty;

    public Subcategory? Subcategory { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Product
{
    public int ProductId { get; set; }
    public int ProductTypeId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; } = 0;
    public decimal? PromotionPrice { get; set; }
    public DateOnly? ExpiredDate { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? WeightOrVolume { get; set; }
    public string? Unit { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "Available";
    public int? SubstituteProductId { get; set; }

    public ProductType? ProductType { get; set; }
    public Product? SubstituteProduct { get; set; }
    public ICollection<ProductHealthTag> ProductHealthTags { get; set; } = new List<ProductHealthTag>();
    public ICollection<ProductSlot> ProductSlots { get; set; } = new List<ProductSlot>();
    public ICollection<InvoiceHistoryItem> InvoiceHistoryItems { get; set; } = new List<InvoiceHistoryItem>();
    public ICollection<MealItem> MealItems { get; set; } = new List<MealItem>();
    public ICollection<SponsoredProduct> SponsoredProducts { get; set; } = new List<SponsoredProduct>();
}

public class ProductHealthTag
{
    public int ProductId { get; set; }
    public int HealthTagId { get; set; }

    public Product? Product { get; set; }
    public HealthTag? HealthTag { get; set; }
}

// ════════════════════════════════════════════════════════════════════════════
// REGION 3: Shopping & Meal
// ════════════════════════════════════════════════════════════════════════════

public class InvoiceHistory
{
    public int InvoiceHistoryId { get; set; }
    public int MemberId { get; set; }
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public decimal TotalPrice { get; set; } = 0;
    public string? PaymentMethod { get; set; }
    public int PointsEarned { get; set; } = 0;

    public Member? Member { get; set; }
    public ICollection<InvoiceHistoryItem> InvoiceHistoryItems { get; set; } = new List<InvoiceHistoryItem>();
}

public class InvoiceHistoryItem
{
    public int InvoiceHistoryItemId { get; set; }
    public int InvoiceHistoryId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; } = 0;

    public InvoiceHistory? InvoiceHistory { get; set; }
    public Product? Product { get; set; }
}

public class MealSuggestion
{
    public int MealSuggestionId { get; set; }
    public string MealName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int YieldPortions { get; set; } = 1;
    public string? ImageUrl { get; set; }
    public int? Calories { get; set; }
    public decimal? HealthyScore { get; set; }
    public string? AlternativeSuggestion { get; set; }

    public ICollection<MealItem> MealItems { get; set; } = new List<MealItem>();
}

public class MealItem
{
    public int MealSuggestionId { get; set; }
    public int ProductId { get; set; }
    public decimal QuantityRequired { get; set; } = 1;
    public string? UnitOfMeasure { get; set; }

    public MealSuggestion? MealSuggestion { get; set; }
    public Product? Product { get; set; }
}

// ════════════════════════════════════════════════════════════════════════════
// REGION 4: Store Layout  (Floor → Zone → Aisle → Shelf → Slot)
// ════════════════════════════════════════════════════════════════════════════

public class Floor
{
    public int FloorId { get; set; }
    public int FloorNumber { get; set; } = 1;
    public string? FloorName { get; set; }

    public ICollection<Zone> Zones { get; set; } = new List<Zone>();
    public ICollection<Map> Maps { get; set; } = new List<Map>();
}

public class Zone
{
    public int ZoneId { get; set; }
    public int FloorId { get; set; }
    public string? ZoneName { get; set; }
    public string? Description { get; set; }
    public double? XMin { get; set; }
    public double? YMin { get; set; }
    public double? XMax { get; set; }
    public double? YMax { get; set; }

    public Floor? Floor { get; set; }
    public ICollection<Aisle> Aisles { get; set; } = new List<Aisle>();
    public ICollection<RobotZone> RobotZones { get; set; } = new List<RobotZone>();
}

public class Aisle
{
    public int AisleId { get; set; }
    public int ZoneId { get; set; }
    public string AisleCode { get; set; } = string.Empty;
    public string? AisleName { get; set; }

    public Zone? Zone { get; set; }
    public ICollection<Shelf> Shelves { get; set; } = new List<Shelf>();
    public ICollection<AisleNode> AisleNodes { get; set; } = new List<AisleNode>();
    public ICollection<AisleScan> AisleScans { get; set; } = new List<AisleScan>();
}

public class Shelf
{
    public int ShelfId { get; set; }
    public int AisleId { get; set; }
    public int LevelNumber { get; set; } = 1;   // 1=bottom, 2=mid, 3=top
    public string? ShelfName { get; set; }

    public Aisle? Aisle { get; set; }
    public ICollection<Slot> Slots { get; set; } = new List<Slot>();
}

public class Slot
{
    public int SlotId { get; set; }
    public int ShelfId { get; set; }
    public string? SlotCode { get; set; }   // ví dụ: "A1-L2-S3"
    public int Quantity { get; set; } = 0;
    public DateTime? LastScannedAt { get; set; }

    public Shelf? Shelf { get; set; }
    public ICollection<ProductSlot> ProductSlots { get; set; } = new List<ProductSlot>();
}

/// <summary>Junction: nhiều sản phẩm có thể nằm trong 1 slot (khuyến mãi, bundle).</summary>
public class ProductSlot
{
    public int ProductSlotId { get; set; }
    public int SlotId { get; set; }
    public int ProductId { get; set; }
    public int ProductQuantity { get; set; } = 1;

    public Slot? Slot { get; set; }
    public Product? Product { get; set; }
}

// ════════════════════════════════════════════════════════════════════════════
// REGION 5: Advertising & Sponsorship
// ════════════════════════════════════════════════════════════════════════════

public class Brand
{
    public int BrandId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public decimal Wallet { get; set; } = 0;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }

    public ICollection<AdCampaign> AdCampaigns { get; set; } = new List<AdCampaign>();
}

public class AdPackage
{
    public int PackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public decimal PricePackage { get; set; } = 0;
    public decimal PriceRoute { get; set; } = 0;
    public decimal BasePriceClick { get; set; } = 0;
    public int AdScore { get; set; } = 0;
    public string Status { get; set; } = "Active";

    public ICollection<AdCampaign> AdCampaigns { get; set; } = new List<AdCampaign>();
}

/// <summary>Gán 1 robot vào 1 zone phục vụ quảng cáo.</summary>
public class RobotZone
{
    public int RobotZoneId { get; set; }
    public int RobotId { get; set; }
    public int ZoneId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public Robot? Robot { get; set; }
    public Zone? Zone { get; set; }
}

public class AdCampaign
{
    public int AdCampaignId { get; set; }
    public int PackageId { get; set; }
    public int BrandId { get; set; }
    public int? RobotZoneId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "Active";
    public decimal Budget { get; set; } = 0;
    public decimal Spent { get; set; } = 0;

    public AdPackage? Package { get; set; }
    public Brand? Brand { get; set; }
    public RobotZone? RobotZone { get; set; }
    public ICollection<SponsoredProduct> SponsoredProducts { get; set; } = new List<SponsoredProduct>();
    public ICollection<AdResource> AdResources { get; set; } = new List<AdResource>();
    public ICollection<AdCampaignLog> AdCampaignLogs { get; set; } = new List<AdCampaignLog>();
}

public class SponsoredProduct
{
    public int SponsoredId { get; set; }
    public int AdCampaignId { get; set; }
    public int ProductId { get; set; }
    public int Priority { get; set; } = 0;
    public string Status { get; set; } = "Active";

    public AdCampaign? AdCampaign { get; set; }
    public Product? Product { get; set; }
}

public class AdResource
{
    public int ResourceId { get; set; }
    public int AdCampaignId { get; set; }
    public string ResourceType { get; set; } = string.Empty;   // image | video | text
    public string ResourceUrl { get; set; } = string.Empty;
    public string? ContentText { get; set; }
    public string? Resolution { get; set; }
    public string Status { get; set; } = "Active";

    public AdCampaign? AdCampaign { get; set; }
}

/// <summary>Log tính phí quảng cáo (RoutePass / Click / View).</summary>
public class AdCampaignLog
{
    public int LogId { get; set; }
    public int AdCampaignId { get; set; }
    public string ActionType { get; set; } = string.Empty;   // RoutePass | Click | View
    public decimal ChargedAmount { get; set; } = 0;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    // Phase B — RoutePass metadata (nullable)
    public int? SponsoredId { get; set; }
    public int? ProductId { get; set; }
    public int? RobotId { get; set; }
    public int? RobotZoneId { get; set; }
    public int? ZoneId { get; set; }
    public int? SlotId { get; set; }
    public int? MemberId { get; set; }
    public string? SessionId { get; set; }
    public double? XCoord { get; set; }
    public double? YCoord { get; set; }

    public AdCampaign? AdCampaign { get; set; }
    public SponsoredProduct? SponsoredProduct { get; set; }
    public Product? Product { get; set; }
    public Robot? Robot { get; set; }
    public RobotZone? RobotZone { get; set; }
    public Zone? Zone { get; set; }
    public Slot? Slot { get; set; }
    public Member? Member { get; set; }
}

// ════════════════════════════════════════════════════════════════════════════
// REGION 6: Robot & Navigation  ← DÙNG CHO ESP32 + React Map Editor
// ════════════════════════════════════════════════════════════════════════════

/// <summary>Robot đẩy hàng — giao tiếp qua MQTT hoặc HTTP về BE.</summary>
public class Robot
{
    public int RobotId { get; set; }
    public string RobotName { get; set; } = string.Empty;
    public string RobotCode { get; set; } = string.Empty;   // ví dụ: "R001"
    public int BatteryPct { get; set; } = 100;
    /// <summary>idle | navigating | scanning | charging | returning</summary>
    public string Mode { get; set; } = "idle";
    /// <summary>Online | Offline | Maintenance | Error</summary>
    public string Status { get; set; } = "Offline";
    public DateTime? LastSeenAt { get; set; }
    public string? IPAddress { get; set; }
    public string? FirmwareVer { get; set; }
    public double? CurrentX { get; set; }
    public double? CurrentY { get; set; }
    public double? CurrentHeading { get; set; }   // radian

    public ICollection<RobotLog> RobotLogs { get; set; } = new List<RobotLog>();
    public ICollection<RobotZone> RobotZones { get; set; } = new List<RobotZone>();
    public ICollection<RobotRoute> RobotRoutes { get; set; } = new List<RobotRoute>();
    public ICollection<RouteAssignment> RouteAssignments { get; set; } = new List<RouteAssignment>();
    public ICollection<AisleScan> AisleScans { get; set; } = new List<AisleScan>();
}

/// <summary>Log trạng thái robot — ESP32 gửi heartbeat định kỳ.</summary>
public class RobotLog
{
    public int LogId { get; set; }
    public int? RobotId { get; set; }
    public int? Battery { get; set; }
    public string? Location { get; set; }
    public string Status { get; set; } = "Idle";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public double? XCoord { get; set; }
    public double? YCoord { get; set; }
    public double? HeadingRad { get; set; }
    public string? ErrorCode { get; set; }
    public string? Message { get; set; }

    public Robot? Robot { get; set; }
}

/// <summary>Bản đồ tĩnh của 1 tầng — lưu grid metadata + ảnh floorplan.</summary>
public class Map
{
    public int MapId { get; set; }
    public int FloorId { get; set; }
    public string MapName { get; set; } = string.Empty;
    public string? MapData { get; set; }   // JSON: { gridWidth, gridHeight, cellSize, originX, originY }
    public string? ImageUrl { get; set; }   // URL ảnh floorplan tĩnh
    public int? GridWidth { get; set; }
    public int? GridHeight { get; set; }
    public double? CellSize { get; set; }   // kích thước 1 ô (m)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Floor? Floor { get; set; }
    public ICollection<NavigationNode> NavigationNodes { get; set; } = new List<NavigationNode>();
    public ICollection<SemanticObject> SemanticObjects { get; set; } = new List<SemanticObject>();
    public ICollection<RobotRoute> RobotRoutes { get; set; } = new List<RobotRoute>();
}

/// <summary>
/// Điểm waypoint trên bản đồ — đỉnh đồ thị cho thuật toán Dijkstra.
/// FE React Map Editor vẽ node này bằng click trên canvas, gửi lên BE lưu.
/// ESP32 nhận danh sách NodeID → tọa độ (X, Y) để điều hướng.
/// </summary>
public class NavigationNode
{
    public int NodeId { get; set; }
    public int MapId { get; set; }
    /// <summary>Tên hiển thị: "N_A1_START", "N_CROSS_02", "N_CHARGE_01"</summary>
    public string? NodeName { get; set; }
    public double XCoord { get; set; }
    public double YCoord { get; set; }
    /// <summary>intersection | aisle | entrance | exit | charging | shelf | virtual</summary>
    public string NodeType { get; set; } = "intersection";
    /// <summary>Tạm khóa — vùng cấm hoặc robot đang đứng.</summary>
    public bool IsBlocked { get; set; } = false;
    /// <summary>Node ảo cho tính toán đường đi (không hiển thị trên map).</summary>
    public bool IsVirtual { get; set; } = false;

    public Map? Map { get; set; }
    public ICollection<NavigationEdge> OutgoingEdges { get; set; } = new List<NavigationEdge>();
    public ICollection<NavigationEdge> IncomingEdges { get; set; } = new List<NavigationEdge>();
    public ICollection<AisleNode> AisleNodes { get; set; } = new List<AisleNode>();
    public ICollection<RouteNodeMapping> RouteNodeMappings { get; set; } = new List<RouteNodeMapping>();
}

/// <summary>
/// Cạnh đồ thị — kết nối 2 NavigationNode.
/// FE React vẽ đường nối giữa 2 node, gửi lên BE lưu.
/// Distance = Euclidean distance (X, Y) hoặc đo thực tế.
/// </summary>
public class NavigationEdge
{
    public int EdgeId { get; set; }
    public int FromNodeId { get; set; }
    public int ToNodeId { get; set; }
    /// <summary>Khoảng cách thực (mét) — dùng cho Dijkstra.</summary>
    public double Distance { get; set; } = 0;
    /// <summary>true = có thể đi ngược lại; false = 1 chiều.</summary>
    public bool IsBidirectional { get; set; } = true;
    /// <summary>Trọng số nhân thêm — vùng đông → cost cao, chậm robot.</summary>
    public double CostMultiplier { get; set; } = 1.0;
    public bool IsBlocked { get; set; } = false;

    public NavigationNode? FromNode { get; set; }
    public NavigationNode? ToNode { get; set; }
}

/// <summary>Map aisle → start/end NavigationNode (FE editor tự gán khi vẽ aisle).</summary>
public class AisleNode
{
    public int AisleNodeId { get; set; }
    public int AisleId { get; set; }
    public int NodeId { get; set; }
    public bool IsStart { get; set; } = true;  // true=đầu lối đi, false=cuối lối đi

    public Aisle? Aisle { get; set; }
    public NavigationNode? Node { get; set; }
}

/// <summary>
/// Lộ trình cố định gồm nhiều node theo thứ tự.
/// Staff tạo route bằng FE Map Editor (kéo thả node).
/// Gán cho robot → RouteAssignment → ESP32 nhận lệnh.
/// </summary>
public class RobotRoute
{
    public int RobotRouteId { get; set; }
    public int RobotId { get; set; }
    public int MapId { get; set; }
    public string? RouteName { get; set; }
    /// <summary>patrol | restock | delivery | custom</summary>
    public string RouteType { get; set; } = "patrol";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Robot? Robot { get; set; }
    public Map? Map { get; set; }
    public ICollection<RouteNodeMapping> RouteNodeMappings { get; set; } = new List<RouteNodeMapping>();
    public ICollection<RouteAssignment> RouteAssignments { get; set; } = new List<RouteAssignment>();
}

/// <summary>
/// Thứ tự các node trong 1 RobotRoute.
/// FE Map Editor sắp xếp trình tự bằng drag-drop.
/// </summary>
public class RouteNodeMapping
{
    public int RouteNodeMappingId { get; set; }
    public int RobotRouteId { get; set; }
    public int NodeId { get; set; }
    public int SequenceOrder { get; set; } = 0;
    /// <summary>Thời gian chờ tại node (giây) — ví dụ: chờ scan shelf.</summary>
    public int WaitTimeSec { get; set; } = 0;
    /// <summary>Lệnh đặc biệt tại node: SCAN | TURN_LEFT | TURN_RIGHT | LIFT_UP | LIFT_DOWN | STOP</summary>
    public string? Instruction { get; set; }

    public RobotRoute? RobotRoute { get; set; }
    public NavigationNode? Node { get; set; }
}

/// <summary>
/// Gán RobotRoute cho Robot để thực thi.
/// ESP32 nhận MQTT message: { routeAssignmentId, nodeSequence[] } → bắt đầu điều hướng.
/// </summary>
public class RouteAssignment
{
    public int RouteAssignmentId { get; set; }
    public int RobotId { get; set; }
    public int RobotRouteId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Pending | InProgress | Completed | Cancelled | Failed</summary>
    public string Status { get; set; } = "Pending";
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }

    public Robot? Robot { get; set; }
    public RobotRoute? RobotRoute { get; set; }
}

/// <summary>
/// Lưu ảnh kệ hàng khi robot scan.
/// ESP32 chụp ảnh, gửi URL lên BE, AI service phân tích EmptyPercentage.
/// </summary>
public class AisleScan
{
    public int ScanId { get; set; }
    public int AisleId { get; set; }
    public int RobotId { get; set; }
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
    public decimal EmptyPercentage { get; set; } = 0;
    public bool NeedsRestock { get; set; } = false;
    public string? ImageUrl { get; set; }
    public double? CameraAngle { get; set; }
    public double? XCoord { get; set; }
    public double? YCoord { get; set; }

    public Aisle? Aisle { get; set; }
    public Robot? Robot { get; set; }
}

/// <summary>
/// Đối tượng ngữ nghĩa phát hiện trên bản đồ.
/// AI vision phát hiện → lưu bounding box → staff xác nhận (IsVerified).
/// </summary>
public class SemanticObject
{
    public int ObjectId { get; set; }
    public int MapId { get; set; }
    /// <summary>shelf | obstacle | entrance | exit | charging_station | person | cart | shelf_empty</summary>
    public string ObjectType { get; set; } = string.Empty;
    public double XMin { get; set; }
    public double YMin { get; set; }
    public double XMax { get; set; }
    public double YMax { get; set; }
    public string? Label { get; set; }
    public double? Confidence { get; set; }
    public DateTime? DetectedAt { get; set; }
    public string? ImageUrl { get; set; }
    /// <summary>Staff xác nhận thì = true → dùng cho navigation.</summary>
    public bool IsVerified { get; set; } = false;

    public Map? Map { get; set; }
}
