-- ============================================================
-- SmartMarketBot — Full Database Script (ERD-based)
-- Tên bảng: SNAKE_CASE theo thiết kế Conceptual ERD mới nhất
-- Target: SQL Server (LocalDB / SQL Server Express / Standard)
-- Múi giờ: Đã chuyển toàn bộ thời gian mặc định sang múi giờ Việt Nam (UTC+7)
-- HƯỚNG DẪN CHẠY: Mở SQL Server Management Studio (SSMS), kết nối 
-- SQL Server của bạn, mở file này và nhấn "Execute" (F5).
-- ============================================================

-- 1. Tự động tạo Database nếu chưa tồn tại
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'SuperMarketBot')
BEGIN
    CREATE DATABASE SuperMarketBot;
END
GO

USE SuperMarketBot;
GO

-- Drop FK constraint first to avoid drop cycle blocks
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_PRODUCT_AD_CAMPAIGN' AND parent_object_id = OBJECT_ID('dbo.PRODUCT'))
BEGIN
    ALTER TABLE dbo.PRODUCT DROP CONSTRAINT FK_PRODUCT_AD_CAMPAIGN;
END
GO

IF OBJECT_ID('dbo.CART_ITEM', 'U') IS NOT NULL DROP TABLE dbo.CART_ITEM;
IF OBJECT_ID('dbo.CART', 'U') IS NOT NULL DROP TABLE dbo.CART;
IF OBJECT_ID('dbo.MEMBER_FAVORITE_PRODUCT', 'U') IS NOT NULL DROP TABLE dbo.MEMBER_FAVORITE_PRODUCT;
IF OBJECT_ID('dbo.MEMBER_NOTIFICATION', 'U') IS NOT NULL DROP TABLE dbo.MEMBER_NOTIFICATION;
IF OBJECT_ID('dbo.AD_CAMPAIGN_LOG', 'U') IS NOT NULL DROP TABLE dbo.AD_CAMPAIGN_LOG;
IF OBJECT_ID('dbo.SPONSORED_PRODUCT', 'U') IS NOT NULL DROP TABLE dbo.SPONSORED_PRODUCT;
IF OBJECT_ID('dbo.AD_RESOURCE', 'U') IS NOT NULL DROP TABLE dbo.AD_RESOURCE;
IF OBJECT_ID('dbo.AD_CAMPAIGN', 'U') IS NOT NULL DROP TABLE dbo.AD_CAMPAIGN;
IF OBJECT_ID('dbo.AD_PACKAGE', 'U') IS NOT NULL DROP TABLE dbo.AD_PACKAGE;
IF OBJECT_ID('dbo.BRAND', 'U') IS NOT NULL DROP TABLE dbo.BRAND;
IF OBJECT_ID('dbo.ROUTE_ASSIGNMENT', 'U') IS NOT NULL DROP TABLE dbo.ROUTE_ASSIGNMENT;
IF OBJECT_ID('dbo.ROUTE_NODE_MAPPING', 'U') IS NOT NULL DROP TABLE dbo.ROUTE_NODE_MAPPING;
IF OBJECT_ID('dbo.ROBOT_ROUTE', 'U') IS NOT NULL DROP TABLE dbo.ROBOT_ROUTE;
IF OBJECT_ID('dbo.ROBOT_ZONE', 'U') IS NOT NULL DROP TABLE dbo.ROBOT_ZONE;
IF OBJECT_ID('dbo.ROBOT_LOG', 'U') IS NOT NULL DROP TABLE dbo.ROBOT_LOG;
IF OBJECT_ID('dbo.AISLE_SCAN', 'U') IS NOT NULL DROP TABLE dbo.AISLE_SCAN;
IF OBJECT_ID('dbo.ROBOT', 'U') IS NOT NULL DROP TABLE dbo.ROBOT;
IF OBJECT_ID('dbo.SEMANTIC_OBJECT', 'U') IS NOT NULL DROP TABLE dbo.SEMANTIC_OBJECT;
IF OBJECT_ID('dbo.AISLE_NODE', 'U') IS NOT NULL DROP TABLE dbo.AISLE_NODE;
IF OBJECT_ID('dbo.NAVIGATION_EDGE', 'U') IS NOT NULL DROP TABLE dbo.NAVIGATION_EDGE;
IF OBJECT_ID('dbo.NAVIGATION_NODE', 'U') IS NOT NULL DROP TABLE dbo.NAVIGATION_NODE;
IF OBJECT_ID('dbo.MAP', 'U') IS NOT NULL DROP TABLE dbo.MAP;
IF OBJECT_ID('dbo.PRODUCT_SLOT', 'U') IS NOT NULL DROP TABLE dbo.PRODUCT_SLOT;
IF OBJECT_ID('dbo.SLOT', 'U') IS NOT NULL DROP TABLE dbo.SLOT;
IF OBJECT_ID('dbo.SHELF', 'U') IS NOT NULL DROP TABLE dbo.SHELF;
IF OBJECT_ID('dbo.AISLE', 'U') IS NOT NULL DROP TABLE dbo.AISLE;
IF OBJECT_ID('dbo.ZONE', 'U') IS NOT NULL DROP TABLE dbo.ZONE;
IF OBJECT_ID('dbo.FLOOR', 'U') IS NOT NULL DROP TABLE dbo.FLOOR;
IF OBJECT_ID('dbo.MEAL_ITEM', 'U') IS NOT NULL DROP TABLE dbo.MEAL_ITEM;
IF OBJECT_ID('dbo.MEAL_SUGGESTION', 'U') IS NOT NULL DROP TABLE dbo.MEAL_SUGGESTION;
IF OBJECT_ID('dbo.INVOICE_HISTORY_ITEM', 'U') IS NOT NULL DROP TABLE dbo.INVOICE_HISTORY_ITEM;
IF OBJECT_ID('dbo.INVOICE_HISTORY', 'U') IS NOT NULL DROP TABLE dbo.INVOICE_HISTORY;
IF OBJECT_ID('dbo.PRODUCT_HEALTHTAG', 'U') IS NOT NULL DROP TABLE dbo.PRODUCT_HEALTHTAG;
IF OBJECT_ID('dbo.PRODUCT', 'U') IS NOT NULL DROP TABLE dbo.PRODUCT;
IF OBJECT_ID('dbo.PRODUCT_TYPE', 'U') IS NOT NULL DROP TABLE dbo.PRODUCT_TYPE;
IF OBJECT_ID('dbo.SUBCATEGORY', 'U') IS NOT NULL DROP TABLE dbo.SUBCATEGORY;
IF OBJECT_ID('dbo.CATEGORY', 'U') IS NOT NULL DROP TABLE dbo.CATEGORY;
IF OBJECT_ID('dbo.MEMBERHEALTH_PREFERENCE', 'U') IS NOT NULL DROP TABLE dbo.MEMBERHEALTH_PREFERENCE;
IF OBJECT_ID('dbo.HEALTH_TAG', 'U') IS NOT NULL DROP TABLE dbo.HEALTH_TAG;
IF OBJECT_ID('dbo.MEMBERSHIP', 'U') IS NOT NULL DROP TABLE dbo.MEMBERSHIP;
IF OBJECT_ID('dbo.MEMBER', 'U') IS NOT NULL DROP TABLE dbo.MEMBER;
IF OBJECT_ID('dbo.ACCOUNT', 'U') IS NOT NULL DROP TABLE dbo.ACCOUNT;
GO

-- ============================================================
-- 1. TÀI KHOẢN & PHÂN QUYỀN
-- ============================================================

CREATE TABLE dbo.ACCOUNT (
    AccountID       INT IDENTITY(1,1) PRIMARY KEY,
    Username        NVARCHAR(100)  NOT NULL UNIQUE,
    PasswordHash    NVARCHAR(500)  NOT NULL,
    Email           NVARCHAR(256)  NOT NULL UNIQUE,
    Phone           NVARCHAR(20)   NULL,
    FullName        NVARCHAR(100)  NULL,
    AvatarUrl       NVARCHAR(500)  NULL,      -- URL ảnh đại diện (Cloudinary/CDN)
    Status          NVARCHAR(50)   NOT NULL,  -- enum (e.g. Active, Inactive, Pending, Blocked)
    Role            NVARCHAR(50)   NOT NULL,  -- Enum (e.g. Admin, Staff, Member)
    OtpCode         NVARCHAR(6)    NULL,      -- Mã OTP xác thực email/quên mật khẩu (Gộp từ EMAIL_OTP)
    OtpExpiredAt    DATETIME2      NULL,      -- Thời gian hết hạn của OTP (Gộp từ EMAIL_OTP)
    OtpType         NVARCHAR(50)   NULL,      -- Loại OTP (e.g. Registration, PasswordReset)
    CreatedAt       DATETIME2      NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE()), -- Múi giờ Việt Nam (UTC+7)
    RefreshToken    NVARCHAR(500)  NULL,      -- JWT refresh token hiện tại (chỉ giữ 1 token/Account)
    RefreshExpiry   DATETIME2      NULL,      -- Thời gian hết hạn refresh token
    IsTokenRevoked  BIT            NOT NULL DEFAULT 0  -- Cờ revoke (logout / reset password)
);

-- ============================================================
-- 2. THÀNH VIÊN & CÁ NHÂN HÓA
-- ============================================================

CREATE TABLE dbo.MEMBER (
    MemberID            INT IDENTITY(1,1) PRIMARY KEY,
    AccountID           INT            NULL REFERENCES dbo.ACCOUNT(AccountID) ON DELETE SET NULL,
    FullName            NVARCHAR(200)  NOT NULL,
    FacePath            NVARCHAR(500)  NULL,
    FaceVector          NVARCHAR(MAX)  NULL,    -- JSON array 128-d vector
    SpendingLimit       DECIMAL(18,2)  NULL,    -- Ngân sách mua sắm
    TotalPoints         INT            NOT NULL DEFAULT 0
);

CREATE TABLE dbo.MEMBERSHIP (
    MembershipID    INT IDENTITY(1,1) PRIMARY KEY,
    MemberID        INT            NOT NULL REFERENCES dbo.MEMBER(MemberID) ON DELETE CASCADE,
    TierName        NVARCHAR(50)   NOT NULL,
    Status          NVARCHAR(50)   NOT NULL  -- enum
);

CREATE TABLE dbo.HEALTH_TAG (
    HealthTagID     INT IDENTITY(1,1) PRIMARY KEY,
    TagName         NVARCHAR(100)  NOT NULL,
    TagType         NVARCHAR(50)   NOT NULL DEFAULT 'diet'  -- diet / allergy / lifestyle
);

CREATE TABLE dbo.MEMBERHEALTH_PREFERENCE (
    MemberID        INT NOT NULL REFERENCES dbo.MEMBER(MemberID) ON DELETE CASCADE,
    HealthTagID     INT NOT NULL REFERENCES dbo.HEALTH_TAG(HealthTagID) ON DELETE CASCADE,
    status          NVARCHAR(50) NOT NULL, -- enum
    PRIMARY KEY (MemberID, HealthTagID)
);

CREATE TABLE dbo.MEMBER_NOTIFICATION (
    NotificationID INT IDENTITY(1,1) PRIMARY KEY,
    MemberID       INT            NOT NULL REFERENCES dbo.MEMBER(MemberID) ON DELETE CASCADE,
    NotifType      NVARCHAR(50)   NOT NULL,  -- Allergy | BudgetExceeded | DuplicatePurchase | CartUpdate | PointsEarned | TestNotification
    Title          NVARCHAR(200)  NOT NULL,
    Message        NVARCHAR(MAX)  NOT NULL,
    PayloadJson    NVARCHAR(MAX)  NULL,      -- JSON payload tùy loại (productId, points...)
    IsRead         BIT            NOT NULL DEFAULT 0,
    CreatedAt      DATETIME2      NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE()) -- Múi giờ Việt Nam (UTC+7)
);
CREATE INDEX IX_MN_MemberID_IsRead ON dbo.MEMBER_NOTIFICATION(MemberID, IsRead);
CREATE INDEX IX_MN_CreatedAt ON dbo.MEMBER_NOTIFICATION(CreatedAt DESC);

CREATE TABLE dbo.MEMBER_FAVORITE_PRODUCT (
    FavoriteID     INT IDENTITY(1,1) PRIMARY KEY,
    MemberID       INT NOT NULL REFERENCES dbo.MEMBER(MemberID) ON DELETE CASCADE,
    ProductID      INT NOT NULL REFERENCES dbo.PRODUCT(ProductID) ON DELETE CASCADE,
    CreatedAt      DATETIME2 NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE()) -- Múi giờ Việt Nam (UTC+7)
);
CREATE UNIQUE INDEX IX_MFP_MemberID_ProductID ON dbo.MEMBER_FAVORITE_PRODUCT(MemberID, ProductID);

-- ============================================================
-- 3. SẢN PHẨM & DANH MỤC
-- ============================================================

CREATE TABLE dbo.CATEGORY (
    CategoryID      INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName    NVARCHAR(100)  NOT NULL,
    Description     NVARCHAR(500)  NULL
);

CREATE TABLE dbo.SUBCATEGORY (
    SubcategoryID   INT IDENTITY(1,1) PRIMARY KEY,
    CategoryID      INT            NOT NULL REFERENCES dbo.CATEGORY(CategoryID) ON DELETE CASCADE,
    SubcategoryName NVARCHAR(100)  NOT NULL
);

CREATE TABLE dbo.PRODUCT_TYPE (
    ProductTypeID   INT IDENTITY(1,1) PRIMARY KEY,
    SubcategoryID   INT            NOT NULL REFERENCES dbo.SUBCATEGORY(SubcategoryID) ON DELETE CASCADE,
    TypeName        NVARCHAR(100)  NOT NULL
);

CREATE TABLE dbo.PRODUCT (
    ProductID           INT IDENTITY(1,1) PRIMARY KEY,
    ProductTypeID       INT            NOT NULL REFERENCES dbo.PRODUCT_TYPE(ProductTypeID),
    ProductName         NVARCHAR(200)  NOT NULL,
    UnitPrice           DECIMAL(18,2)  NOT NULL DEFAULT 0,
    PromotionPrice      DECIMAL(18,2)  NULL,      -- Giá khuyến mãi đã giảm
    ExpiredDate         DATETIME2      NULL,
    ImageUrl            NVARCHAR(500)  NULL,
    WeightOrVolume      DECIMAL(18,3)  NULL,
    Unit                NVARCHAR(20)   NULL,
    Description         NVARCHAR(MAX)  NULL,
    Status              NVARCHAR(50)   NOT NULL, -- enum
    SubstituteProductID INT            NULL REFERENCES dbo.PRODUCT(ProductID)
);

-- Product không còn liên kết trực tiếp đến AdCampaign;
-- quảng cáo sản phẩm được quản lý qua bảng trung gian SPONSORED_PRODUCT.
IF OBJECT_ID('dbo.FK_PRODUCT_AD_CAMPAIGN', 'F') IS NOT NULL
    ALTER TABLE dbo.PRODUCT DROP CONSTRAINT FK_PRODUCT_AD_CAMPAIGN;
GO

CREATE TABLE dbo.PRODUCT_HEALTHTAG (
    ProductID       INT NOT NULL REFERENCES dbo.PRODUCT(ProductID) ON DELETE CASCADE,
    HealthTagID     INT NOT NULL REFERENCES dbo.HEALTH_TAG(HealthTagID) ON DELETE CASCADE,
    PRIMARY KEY (ProductID, HealthTagID)
);

-- ============================================================
-- 4. LỊCH SỬ MUA HÀNG (INVOICES)
-- ============================================================

CREATE TABLE dbo.INVOICE_HISTORY (
    InvoiceHistoryID    INT IDENTITY(1,1) PRIMARY KEY,
    MemberID            INT            NOT NULL REFERENCES dbo.MEMBER(MemberID) ON DELETE CASCADE,
    PurchaseDate        DATETIME2      NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE()), -- Múi giờ Việt Nam (UTC+7)
    TotalPrice          DECIMAL(18,2)  NOT NULL DEFAULT 0
);

CREATE TABLE dbo.INVOICE_HISTORY_ITEM (
    InvoiceHistoryItemID INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceHistoryID     INT            NOT NULL REFERENCES dbo.INVOICE_HISTORY(InvoiceHistoryID) ON DELETE CASCADE,
    ProductID            INT            NOT NULL REFERENCES dbo.PRODUCT(ProductID),
    Quantity             INT            NOT NULL DEFAULT 1,
    UnitPrice            DECIMAL(18,2)  NOT NULL  -- Giá tại thời điểm mua
);

-- ============================================================
-- 5. GỢI Ý THỰC ĐƠN (MEAL SUGGESTIONS / RECIPES)
-- ============================================================

CREATE TABLE dbo.MEAL_SUGGESTION (
    MealSuggestionID        INT IDENTITY(1,1) PRIMARY KEY,
    MealName                NVARCHAR(200)  NOT NULL,
    Description             NVARCHAR(MAX)  NULL,
    YieldPortions           INT            NOT NULL DEFAULT 1,
    ImageUrl                NVARCHAR(500)  NULL,
    Calories                INT            NULL,
    healthy_score           INT            NULL,
    alternative_suggestion  NVARCHAR(500)  NULL
);

CREATE TABLE dbo.MEAL_ITEM (
    MealSuggestionID    INT            NOT NULL REFERENCES dbo.MEAL_SUGGESTION(MealSuggestionID) ON DELETE CASCADE,
    ProductID           INT            NOT NULL REFERENCES dbo.PRODUCT(ProductID),
    QuantityRequired    DECIMAL(18,3)  NOT NULL DEFAULT 1,
    UnitOfMeasure       NVARCHAR(20)   NULL,
    PRIMARY KEY (MealSuggestionID, ProductID)
);

-- ============================================================
-- 6. GIỎ HÀNG
-- ============================================================

CREATE TABLE dbo.CART (
    CartID       INT IDENTITY(1,1) PRIMARY KEY,
    MemberID     INT            NOT NULL UNIQUE REFERENCES dbo.MEMBER(MemberID) ON DELETE CASCADE,
    CreatedAt    DATETIME2      NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE()), -- Múi giờ Việt Nam (UTC+7)
    UpdatedAt    DATETIME2      NULL
);
CREATE INDEX IX_CART_MemberID ON dbo.CART(MemberID);

CREATE TABLE dbo.CART_ITEM (
    CartItemID   INT IDENTITY(1,1) PRIMARY KEY,
    CartID       INT            NOT NULL REFERENCES dbo.CART(CartID) ON DELETE CASCADE,
    ProductID    INT            NOT NULL REFERENCES dbo.PRODUCT(ProductID) ON DELETE CASCADE,
    Quantity     INT            NOT NULL DEFAULT 1,
    AddedAt      DATETIME2      NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE()) -- Múi giờ Việt Nam (UTC+7)
);
CREATE INDEX IX_CART_ITEM_CartID ON dbo.CART_ITEM(CartID);
CREATE INDEX IX_CART_ITEM_ProductID ON dbo.CART_ITEM(ProductID);

-- ============================================================
-- 7. KHÔNG GIAN SIÊU THỊ & KỆ HÀNG
-- ============================================================

CREATE TABLE dbo.FLOOR (
    FloorID         INT IDENTITY(1,1) PRIMARY KEY,
    FloorNumber     INT NOT NULL DEFAULT 1
);

CREATE TABLE dbo.ZONE (
    ZoneID          INT IDENTITY(1,1) PRIMARY KEY,
    FloorID         INT            NOT NULL REFERENCES dbo.FLOOR(FloorID) ON DELETE CASCADE,
    ZoneName        NVARCHAR(100)  NULL,
    Description     NVARCHAR(500)  NULL
);

CREATE TABLE dbo.AISLE (
    AisleID         INT IDENTITY(1,1) PRIMARY KEY,
    ZoneID          INT            NOT NULL REFERENCES dbo.ZONE(ZoneID) ON DELETE CASCADE,
    AisleCode       NVARCHAR(20)   NOT NULL,
    AisleName       NVARCHAR(100)  NULL
);

CREATE TABLE dbo.SHELF (
    ShelfID         INT IDENTITY(1,1) PRIMARY KEY,
    AisleID         INT            NOT NULL REFERENCES dbo.AISLE(AisleID) ON DELETE CASCADE,
    LevelNumber     INT            NOT NULL DEFAULT 1
);

CREATE TABLE dbo.SLOT (
    SlotID          INT IDENTITY(1,1) PRIMARY KEY,
    ShelfID         INT            NOT NULL REFERENCES dbo.SHELF(ShelfID) ON DELETE CASCADE,
    SlotCode        NVARCHAR(20)   NULL,
    Quantity        INT            NOT NULL DEFAULT 0,
    LastScannedAt   DATETIME2      NULL
);

CREATE TABLE dbo.PRODUCT_SLOT (
    ProductsSlotID  INT IDENTITY(1,1) PRIMARY KEY,
    SlotID          INT NOT NULL REFERENCES dbo.SLOT(SlotID) ON DELETE CASCADE,
    ProductID       INT NOT NULL REFERENCES dbo.PRODUCT(ProductID)
);

-- ============================================================
-- 8. BẢN ĐỒ & ĐIỀU HƯỚNG ROBOT
-- ============================================================

CREATE TABLE dbo.MAP (
    MapID               INT IDENTITY(1,1) PRIMARY KEY,
    FloorID             INT            NOT NULL REFERENCES dbo.FLOOR(FloorID),
    MapName             NVARCHAR(100)  NOT NULL,
    MapData             NVARCHAR(MAX)  NULL,    -- JSON layout
    FloorplanImageUrl   NVARCHAR(500)  NULL,    -- Ảnh mặt bằng SLAM từ Tablet (lưu vào Supabase Storage)
    CreatedAt           DATETIME2      NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE()) -- Múi giờ Việt Nam (UTC+7)
);

CREATE TABLE dbo.NAVIGATION_NODE (
    NodeID          INT IDENTITY(1,1) PRIMARY KEY,
    MapID           INT            NOT NULL REFERENCES dbo.MAP(MapID) ON DELETE CASCADE,
    NodeName        NVARCHAR(100)  NULL,
    XCoord          FLOAT          NOT NULL DEFAULT 0,
    YCoord          FLOAT          NOT NULL DEFAULT 0,
    NodeType        NVARCHAR(50)   NULL,
    IsBlocked       BIT            NOT NULL DEFAULT 0
);

CREATE TABLE dbo.NAVIGATION_EDGE (
    EdgeID          INT IDENTITY(1,1) PRIMARY KEY,
    FromNodeID      INT            NOT NULL REFERENCES dbo.NAVIGATION_NODE(NodeID),
    ToNodeID        INT            NOT NULL REFERENCES dbo.NAVIGATION_NODE(NodeID),
    Distance        FLOAT          NOT NULL DEFAULT 0,
    IsBidirectional BIT            NOT NULL DEFAULT 1
);

CREATE TABLE dbo.AISLE_NODE (
    AisleNodeID     INT IDENTITY(1,1) PRIMARY KEY,
    AisleID         INT NOT NULL REFERENCES dbo.AISLE(AisleID),
    NodeID          INT NOT NULL REFERENCES dbo.NAVIGATION_NODE(NodeID)
);

CREATE TABLE dbo.SEMANTIC_OBJECT (
    ObjectID        INT IDENTITY(1,1) PRIMARY KEY,
    MapID           INT            NOT NULL REFERENCES dbo.MAP(MapID) ON DELETE CASCADE,
    ObjectType      NVARCHAR(100)  NOT NULL,
    XMin            FLOAT          NOT NULL DEFAULT 0,
    YMin            FLOAT          NOT NULL DEFAULT 0,
    XMax            FLOAT          NOT NULL DEFAULT 0,
    YMax            FLOAT          NOT NULL DEFAULT 0,
    Label           NVARCHAR(100)  NULL,
    Confidence      FLOAT          NULL,
    DetectedAt      DATETIME2      NULL,
    ImageUrl        NVARCHAR(500)  NULL,
    -- Phase B+: Map Management — gán sản phẩm vào kệ trên map (ProductID → SemanticObject)
    ProductID       INT            NULL REFERENCES dbo.PRODUCT(ProductID) ON DELETE SET NULL
);

CREATE TABLE dbo.ROBOT (
    RobotID         INT IDENTITY(1,1) PRIMARY KEY,
    RobotName       NVARCHAR(100)  NOT NULL,
    RobotCode       NVARCHAR(50)   NOT NULL UNIQUE,
    BatteryPct      INT            NOT NULL DEFAULT 100,
    Mode            NVARCHAR(50)   NOT NULL DEFAULT 'idle',  -- idle / navigating / scanning / charging
    Status          NVARCHAR(50)   NOT NULL,  -- enum
    LastSeenAt      DATETIME2      NULL,
    IPAddress       NVARCHAR(45)   NULL       -- IPv4/IPv6 của robot
);

CREATE TABLE dbo.ROBOT_LOG (
    LogID           INT IDENTITY(1,1) PRIMARY KEY,
    RobotID         INT            NULL REFERENCES dbo.ROBOT(RobotID),
    Battery         INT            NULL,
    Location        NVARCHAR(200)  NULL,
    Status          NVARCHAR(50)   NOT NULL, -- Enum
    Timestamp       DATETIME2      NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE()), -- Múi giờ Việt Nam (UTC+7)
    XCoord          FLOAT          NULL,
    YCoord          FLOAT          NULL,
    HeadingRad      FLOAT          NULL
);

CREATE TABLE dbo.ROBOT_ROUTE (
    RobotRouteID    INT IDENTITY(1,1) PRIMARY KEY,
    RobotID         INT            NOT NULL REFERENCES dbo.ROBOT(RobotID),
    MapID           INT            NOT NULL REFERENCES dbo.MAP(MapID),
    RouteName       NVARCHAR(200)  NULL,
    CreatedAt       DATETIME2      NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE()) -- Múi giờ Việt Nam (UTC+7)
);

CREATE TABLE dbo.ROUTE_NODE_MAPPING (
    RouteNodeMappingID  INT IDENTITY(1,1) PRIMARY KEY,
    RobotRouteID        INT NOT NULL REFERENCES dbo.ROBOT_ROUTE(RobotRouteID) ON DELETE CASCADE,
    NodeID              INT NOT NULL REFERENCES dbo.NAVIGATION_NODE(NodeID),
    SequenceOrder       INT NOT NULL DEFAULT 0
);

CREATE TABLE dbo.ROUTE_ASSIGNMENT (
    RouteAssignmentID   INT IDENTITY(1,1) PRIMARY KEY,
    RobotID             INT            NOT NULL REFERENCES dbo.ROBOT(RobotID),
    RobotRouteID        INT            NOT NULL REFERENCES dbo.ROBOT_ROUTE(RobotRouteID),
    AssignedAt          DATETIME2      NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE()), -- Múi giờ Việt Nam (UTC+7)
    Status              NVARCHAR(50)   NOT NULL DEFAULT 'Pending'  -- Pending / Active / Completed
);

CREATE TABLE dbo.ROBOT_ZONE (
    RobotZoneID     INT IDENTITY(1,1) PRIMARY KEY,
    RobotID         INT NOT NULL REFERENCES dbo.ROBOT(RobotID) ON DELETE CASCADE,
    ZoneID          INT NOT NULL REFERENCES dbo.ZONE(ZoneID)
);

CREATE TABLE dbo.AISLE_SCAN (
    ScanID              INT IDENTITY(1,1) PRIMARY KEY,
    AisleID             INT            NOT NULL REFERENCES dbo.AISLE(AisleID),
    RobotID             INT            NOT NULL REFERENCES dbo.ROBOT(RobotID),
    ScannedAt           DATETIME2      NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE()), -- Múi giờ Việt Nam (UTC+7)
    EmptyPercentage     DECIMAL(5,2)   NOT NULL DEFAULT 0,
    DensityPercentage   DECIMAL(5,2)   NOT NULL DEFAULT 100, -- Phần trăm mật độ còn lại
    NeedsRestock        BIT            NOT NULL DEFAULT 0,
    ImageUrl            NVARCHAR(500)  NULL
);

-- ============================================================
-- 9. THƯƠNG HIỆU & QUẢNG CÁO
-- ============================================================

CREATE TABLE dbo.BRAND (
    BrandID         INT IDENTITY(1,1) PRIMARY KEY,
    BrandName       NVARCHAR(100)  NOT NULL,
    Wallet          DECIMAL(18,2)  NOT NULL DEFAULT 0,
    Description     NVARCHAR(500)  NULL,
    IsSystemBrand   BIT            NOT NULL DEFAULT 0   -- 1 = brand siêu thị tự chạy quảng cáo miễn phí
);

CREATE TABLE dbo.AD_PACKAGE (
    PackageID       INT IDENTITY(1,1) PRIMARY KEY,
    PackageName     NVARCHAR(100)  NOT NULL,
    PricePackage    DECIMAL(18,2)  NOT NULL DEFAULT 0, -- phí cố định / campaign (charge 1 lần)
    PriceRoute      DECIMAL(18,2)  NOT NULL DEFAULT 0, -- đơn giá / 1 route (charge × count)
    PriceZone       DECIMAL(18,2)  NOT NULL DEFAULT 0, -- đơn giá / 1 zone  (charge × count)
    PriceShelf      DECIMAL(18,2)  NOT NULL DEFAULT 0, -- đơn giá / 1 shelf (charge × 1 nếu có SemanticObjectID)
    BasePriceClick  DECIMAL(18,2)  NOT NULL DEFAULT 0,
    AdScore         INT            NOT NULL DEFAULT 0,
    Status          NVARCHAR(50)   NOT NULL  -- enum
);

CREATE TABLE dbo.AD_CAMPAIGN (
    AdCampaignID      INT IDENTITY(1,1) PRIMARY KEY,
    PackageID         INT            NOT NULL REFERENCES dbo.AD_PACKAGE(PackageID),
    BrandID           INT            NOT NULL REFERENCES dbo.BRAND(BrandID),
    SemanticObjectID  INT            NULL REFERENCES dbo.SEMANTIC_OBJECT(ObjectID) ON DELETE SET NULL, -- Kệ (shelf) cụ thể để phát quảng cáo
    CampaignName      NVARCHAR(200)  NOT NULL,
    StartDate         DATETIME2      NOT NULL,
    EndDate           DATETIME2      NOT NULL,
    Status            NVARCHAR(50)   NOT NULL,  -- enum: Inactive | Active | Paused | Completed | Canceled
    -- Snapshot giá shelf tại lúc brand mua:
    ShelfPriceCharged DECIMAL(18,2)  NOT NULL DEFAULT 0,
    ShelfPurchasedAt  DATETIME2      NULL
);
CREATE INDEX IX_AD_CAMPAIGN_Status_Dates ON dbo.AD_CAMPAIGN(Status, StartDate, EndDate);

-- Liên kết N-N: AdCampaign ↔ Zone (đơn giá × số zone)
CREATE TABLE dbo.AD_CAMPAIGN_ZONE (
    AdCampaignID      INT            NOT NULL REFERENCES dbo.AD_CAMPAIGN(AdCampaignID) ON DELETE CASCADE,
    ZoneID            INT            NOT NULL REFERENCES dbo.ZONE(ZoneID)              ON DELETE CASCADE,
    ZonePriceCharged  DECIMAL(18,2)  NOT NULL DEFAULT 0, -- snapshot giá tại lúc mua zone
    PurchasedAt       DATETIME2      NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE()),
    CONSTRAINT PK_AD_CAMPAIGN_ZONE PRIMARY KEY (AdCampaignID, ZoneID)
);
CREATE INDEX IX_AD_CAMPAIGN_ZONE_ZoneID ON dbo.AD_CAMPAIGN_ZONE(ZoneID);

-- Liên kết N-N: AdCampaign ↔ RobotRoute (đơn giá × số route)
CREATE TABLE dbo.AD_CAMPAIGN_ROUTE (
    AdCampaignID        INT            NOT NULL REFERENCES dbo.AD_CAMPAIGN(AdCampaignID) ON DELETE CASCADE,
    RobotRouteID        INT            NOT NULL REFERENCES dbo.ROBOT_ROUTE(RobotRouteID)   ON DELETE CASCADE,
    RoutePriceCharged   DECIMAL(18,2)  NOT NULL DEFAULT 0, -- snapshot giá tại lúc mua route
    PurchasedAt         DATETIME2      NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE()),
    CONSTRAINT PK_AD_CAMPAIGN_ROUTE PRIMARY KEY (AdCampaignID, RobotRouteID)
);
CREATE INDEX IX_AD_CAMPAIGN_ROUTE_RouteID ON dbo.AD_CAMPAIGN_ROUTE(RobotRouteID);

CREATE TABLE dbo.AD_CAMPAIGN_LOG (
    LogID            INT IDENTITY(1,1) PRIMARY KEY,
    AdCampaignID     INT            NOT NULL REFERENCES dbo.AD_CAMPAIGN(AdCampaignID) ON DELETE CASCADE,
    ActionType       NVARCHAR(50)   NOT NULL,  -- Click | Navigation | Impression | RoutePass | FraudDetected
    ChargedAmount    DECIMAL(18,2)  NOT NULL DEFAULT 0, -- Số tiền thực tế bị trừ của lượt tương tác
    Timestamp        DATETIME2      NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE()), -- Múi giờ Việt Nam (UTC+7)
    -- Chi tiết bổ sung theo loại tương tác (nullable để không phá dữ liệu cũ)
    SponsoredID      INT            NULL,
    ProductID        INT            NULL REFERENCES dbo.PRODUCT(ProductID),
    RobotID          INT            NULL REFERENCES dbo.ROBOT(RobotID),
    SemanticObjectID INT            NULL REFERENCES dbo.SEMANTIC_OBJECT(ObjectID) ON DELETE SET NULL,
    ZoneID           INT            NULL REFERENCES dbo.ZONE(ZoneID),
    SlotID           INT            NULL REFERENCES dbo.SLOT(SlotID),
    MemberID         INT            NULL REFERENCES dbo.MEMBER(MemberID) ON DELETE SET NULL,
    SessionID        NVARCHAR(100)  NULL, -- Mã phiên tự sinh từ Robot để chống click-tặc ẩn danh
    XCoord           INT            NULL,
    YCoord           INT            NULL
);
CREATE INDEX IX_AD_CAMPAIGN_LOG_CAMPAIGN ON dbo.AD_CAMPAIGN_LOG(AdCampaignID);
CREATE INDEX IX_AD_CAMPAIGN_LOG_ZONE     ON dbo.AD_CAMPAIGN_LOG(ZoneID, Timestamp DESC);
CREATE INDEX IX_AD_CAMPAIGN_LOG_SESSION  ON dbo.AD_CAMPAIGN_LOG(SessionID, Timestamp DESC);

CREATE TABLE dbo.AD_RESOURCE (
    ResourceID      INT IDENTITY(1,1) PRIMARY KEY,
    AdCampaignID    INT            NOT NULL REFERENCES dbo.AD_CAMPAIGN(AdCampaignID) ON DELETE CASCADE,
    ResourceType    NVARCHAR(20)   NOT NULL,  -- IMAGE | VIDEO | VOICE_TEXT
    ResourceURL     NVARCHAR(500)  NOT NULL,
    ContentText     NVARCHAR(MAX)  NULL,      -- Văn bản cho VOICE_TEXT hoặc caption
    Resolution      NVARCHAR(20)   NULL,      -- ví dụ: 1920x1080
    Status          NVARCHAR(50)   NOT NULL DEFAULT 'Active' -- Active | Inactive
);
CREATE INDEX IX_AD_RESOURCE_CAMPAIGN ON dbo.AD_RESOURCE(AdCampaignID);

CREATE TABLE dbo.SPONSORED_PRODUCT (
    SponsoredID     INT IDENTITY(1,1) PRIMARY KEY,
    AdCampaignID    INT            NOT NULL REFERENCES dbo.AD_CAMPAIGN(AdCampaignID),
    ProductID       INT            NOT NULL REFERENCES dbo.PRODUCT(ProductID) ON DELETE CASCADE,
    Priority        INT            NOT NULL DEFAULT 0,
    status          NVARCHAR(50)   NOT NULL  -- enum
);
CREATE INDEX IX_SPONSORED_PRODUCT_CAMPAIGN ON dbo.SPONSORED_PRODUCT(AdCampaignID);

GO

-- Nối sau cùng để tránh lỗi vòng lặp phụ thuộc (SPONSORED_PRODUCT tạo sau AD_CAMPAIGN_LOG)
ALTER TABLE dbo.AD_CAMPAIGN_LOG ADD CONSTRAINT FK_AD_CAMPAIGN_LOG_SPONSORED
    FOREIGN KEY (SponsoredID) REFERENCES dbo.SPONSORED_PRODUCT(SponsoredID) ON DELETE SET NULL;
GO

-- ============================================================
-- TỔNG KẾT: 42 bảng
-- ============================================================
-- 1.  ACCOUNT
-- 2.  MEMBER
-- 3.  MEMBERSHIP
-- 4.  HEALTH_TAG
-- 5.  MEMBERHEALTH_PREFERENCE
-- 6.  MEMBER_NOTIFICATION
-- 7.  MEMBER_FAVORITE_PRODUCT
-- 8.  CATEGORY
-- 9.  SUBCATEGORY
-- 10. PRODUCT_TYPE
-- 11. PRODUCT
-- 12. PRODUCT_HEALTHTAG
-- 13. INVOICE_HISTORY
-- 14. INVOICE_HISTORY_ITEM
-- 15. MEAL_SUGGESTION
-- 16. MEAL_ITEM
-- 17. CART
-- 18. CART_ITEM
-- 19. FLOOR
-- 20. ZONE
-- 21. AISLE
-- 22. SHELF
-- 23. SLOT
-- 24. PRODUCT_SLOT
-- 25. MAP
-- 26. NAVIGATION_NODE
-- 27. NAVIGATION_EDGE
-- 28. AISLE_NODE
-- 29. SEMANTIC_OBJECT
-- 30. ROBOT
-- 31. ROBOT_LOG
-- 32. ROBOT_ROUTE
-- 33. ROUTE_NODE_MAPPING
-- 34. ROUTE_ASSIGNMENT
-- 35. ROBOT_ZONE
-- 36. AISLE_SCAN
-- 37. BRAND
-- 38. AD_PACKAGE
-- 39. AD_CAMPAIGN
-- 40. AD_CAMPAIGN_LOG
-- 41. SPONSORED_PRODUCT
-- 42. AD_RESOURCE
-- ============================================================
