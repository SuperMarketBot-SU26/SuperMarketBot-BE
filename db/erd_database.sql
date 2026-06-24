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

IF OBJECT_ID('dbo.AD_CAMPAIGN_LOG', 'U') IS NOT NULL DROP TABLE dbo.AD_CAMPAIGN_LOG;
IF OBJECT_ID('dbo.SPONSORED_PRODUCT', 'U') IS NOT NULL DROP TABLE dbo.SPONSORED_PRODUCT;
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

-- ============================================================
-- 3. SẢN PHẨM & DANH MỤC
-- ============================================================

CREATE TABLE dbo.CATEGORY (
    CategoryID      INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName    NVARCHAR(100)  NOT NULL
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
    AdCampaignID        INT            NULL,      -- Liên kết chiến dịch quảng cáo/khuyến mãi (Nối ngoại ở cuối file)
    ExpiredDate         DATETIME2      NULL,
    ImageUrl            NVARCHAR(500)  NULL,
    WeightOrVolume      DECIMAL(18,3)  NULL,
    Unit                NVARCHAR(20)   NULL,
    Description         NVARCHAR(MAX)  NULL,
    Status              NVARCHAR(50)   NOT NULL, -- enum
    SubstituteProductID INT            NULL REFERENCES dbo.PRODUCT(ProductID)
);

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
-- 6. KHÔNG GIAN SIÊU THỊ & KỆ HÀNG
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
-- 7. BẢN ĐỒ & ĐIỀU HƯỚNG ROBOT
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
    LastSeenAt      DATETIME2      NULL
);

CREATE TABLE dbo.ROBOT_LOG (
    LogID           INT IDENTITY(1,1) PRIMARY KEY,
    RobotID         INT            NULL REFERENCES dbo.ROBOT(RobotID),
    battery         INT            NULL,
    location        NVARCHAR(200)  NULL,
    status          NVARCHAR(50)   NOT NULL, -- Enum
    timestamp       DATETIME2      NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE()), -- Múi giờ Việt Nam (UTC+7)
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
    NeedsRestock        BIT            NOT NULL DEFAULT 0,
    ImageUrl            NVARCHAR(500)  NULL
);

-- ============================================================
-- 8. THƯƠNG HIỆU & QUẢNG CÁO
-- ============================================================

CREATE TABLE dbo.BRAND (
    BrandID         INT IDENTITY(1,1) PRIMARY KEY,
    BrandName       NVARCHAR(100)  NOT NULL,
    Wallet          DECIMAL(18,2)  NOT NULL DEFAULT 0,
    Description     NVARCHAR(500)  NULL
);

CREATE TABLE dbo.AD_PACKAGE (
    PackageID       INT IDENTITY(1,1) PRIMARY KEY,
    PackageName     NVARCHAR(100)  NOT NULL,
    PricePackage    DECIMAL(18,2)  NOT NULL DEFAULT 0,
    PriceRoute      DECIMAL(18,2)  NOT NULL DEFAULT 0,
    BasePriceClick  DECIMAL(18,2)  NOT NULL DEFAULT 0,
    AdScore         INT            NOT NULL DEFAULT 0,
    Status          NVARCHAR(50)   NOT NULL  -- enum
);

CREATE TABLE dbo.AD_CAMPAIGN (
    AdCampaignID    INT IDENTITY(1,1) PRIMARY KEY,
    PackageID       INT            NOT NULL REFERENCES dbo.AD_PACKAGE(PackageID),
    BrandID         INT            NOT NULL REFERENCES dbo.BRAND(BrandID),
    RobotZoneID     INT            NULL REFERENCES dbo.ROBOT_ZONE(RobotZoneID) ON DELETE SET NULL, -- Cho phép NULL để tăng linh hoạt (Đã sửa)
    CampaignName    NVARCHAR(200)  NOT NULL,
    StartDate       DATETIME2      NOT NULL,
    EndDate         DATETIME2      NOT NULL,
    Status          NVARCHAR(50)   NOT NULL  -- enum
);

CREATE TABLE dbo.AD_CAMPAIGN_LOG (
    LogID           INT IDENTITY(1,1) PRIMARY KEY,
    AdCampaignID    INT            NOT NULL REFERENCES dbo.AD_CAMPAIGN(AdCampaignID) ON DELETE CASCADE,
    ActionType      NVARCHAR(50)   NOT NULL,  -- Click / View / RoutePass
    ChargedAmount   DECIMAL(18,2)  NOT NULL DEFAULT 0, -- Số tiền thực tế bị trừ của lượt tương tác
    Timestamp       DATETIME2      NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE()), -- Múi giờ Việt Nam (UTC+7)
    -- Phase B+: chi tiết cho ActionType = 'RoutePass' (nullable, không phá dữ liệu Click/View cũ)
    SponsoredID     INT            NULL,
    ProductID       INT            NULL REFERENCES dbo.PRODUCT(ProductID),
    RobotID         INT            NULL REFERENCES dbo.ROBOT(RobotID),
    RobotZoneID     INT            NULL REFERENCES dbo.ROBOT_ZONE(RobotZoneID) ON DELETE SET NULL,
    ZoneID          INT            NULL REFERENCES dbo.ZONE(ZoneID),
    SlotID          INT            NULL REFERENCES dbo.SLOT(SlotID),
    MemberID        INT            NULL REFERENCES dbo.MEMBER(MemberID) ON DELETE SET NULL,
    SessionID       NVARCHAR(100)  NULL, -- Mã phiên tự sinh từ Robot để chống click-tặc ẩn danh
    XCoord          INT            NULL,
    YCoord          INT            NULL
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

-- Xóa ràng buộc FK_PRODUCT_AD_CAMPAIGN vì Product không còn liên kết trực tiếp đến AdCampaign
-- Việc tài trợ sản phẩm được quản lý qua bảng trung gian SPONSORED_PRODUCT
IF OBJECT_ID('dbo.FK_PRODUCT_AD_CAMPAIGN', 'F') IS NOT NULL
    ALTER TABLE dbo.PRODUCT DROP CONSTRAINT FK_PRODUCT_AD_CAMPAIGN;
GO

-- Nối sau cùng để tránh lỗi vòng lặp phụ thuộc (SPONSORED_PRODUCT tạo sau AD_CAMPAIGN_LOG)
ALTER TABLE dbo.AD_CAMPAIGN_LOG ADD CONSTRAINT FK_AD_CAMPAIGN_LOG_SPONSORED
    FOREIGN KEY (SponsoredID) REFERENCES dbo.SPONSORED_PRODUCT(SponsoredID) ON DELETE SET NULL;
GO

-- CART & CART_ITEM (Shopping Cart Option A)
CREATE TABLE dbo.CART (
    CartID       INT IDENTITY(1,1) PRIMARY KEY,
    MemberID     INT            NOT NULL UNIQUE REFERENCES dbo.MEMBER(MemberID) ON DELETE CASCADE,
    CreatedAt    DATETIME2      NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE()),
    UpdatedAt    DATETIME2      NULL
);
CREATE INDEX IX_CART_MemberID ON dbo.CART(MemberID);

CREATE TABLE dbo.CART_ITEM (
    CartItemID   INT IDENTITY(1,1) PRIMARY KEY,
    CartID       INT            NOT NULL REFERENCES dbo.CART(CartID) ON DELETE CASCADE,
    ProductID    INT            NOT NULL REFERENCES dbo.PRODUCT(ProductID) ON DELETE CASCADE,
    Quantity     INT            NOT NULL DEFAULT 1,
    AddedAt      DATETIME2      NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE())
);
CREATE INDEX IX_CART_ITEM_CartID ON dbo.CART_ITEM(CartID);
CREATE INDEX IX_CART_ITEM_ProductID ON dbo.CART_ITEM(ProductID);
GO

-- ============================================================
-- TỔNG KẾT: 39 bảng (Đã thêm CART và CART_ITEM)
-- ============================================================
-- 1.  ACCOUNT
-- 2.  MEMBER
-- 3.  MEMBERSHIP
-- 4.  HEALTH_TAG
-- 5.  MEMBERHEALTH_PREFERENCE
-- 6.  CATEGORY
-- 7.  SUBCATEGORY
-- 8.  PRODUCT_TYPE
-- 9.  PRODUCT
-- 10. PRODUCT_HEALTHTAG
-- 11. INVOICE_HISTORY
-- 12. INVOICE_HISTORY_ITEM
-- 13. MEAL_SUGGESTION
-- 14. MEAL_ITEM
-- 15. FLOOR
-- 16. ZONE
-- 17. AISLE
-- 18. SHELF
-- 19. SLOT
-- 20. PRODUCT_SLOT
-- 21. MAP
-- 22. NAVIGATION_NODE
-- 23. NAVIGATION_EDGE
-- 24. AISLE_NODE
-- 25. SEMANTIC_OBJECT
-- 26. ROBOT
-- 27. ROBOT_LOG
-- 28. ROBOT_ROUTE
-- 29. ROUTE_NODE_MAPPING
-- 30. ROUTE_ASSIGNMENT
-- 31. ROBOT_ZONE
-- 32. AISLE_SCAN
-- 33. BRAND
-- 34. AD_PACKAGE
-- 35. AD_CAMPAIGN
-- 36. AD_CAMPAIGN_LOG (mở rộng Phase B+: SessionID, ChargedAmount, MemberID)
-- 37. SPONSORED_PRODUCT
-- 38. AD_RESOURCE (lưu trữ nội dung đa phương tiện: IMAGE/VIDEO/VOICE_TEXT)
-- ============================================================
