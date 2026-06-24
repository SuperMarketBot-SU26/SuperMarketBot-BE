-- ============================================================================
-- SmartMarketBot — Full DDL (37 bảng, ERD V4.0)
-- Database: SuperMarketBot (SQL Server / Azure SQL)
-- Default thời gian: DATEADD(hour, 7, GETUTCDATE()) — UTC+7 (Việt Nam)
-- ============================================================================

-- ────────────────────────────────────────────────────────────────────────────
-- REGION 1: Customer & Identity
-- ────────────────────────────────────────────────────────────────────────────

CREATE TABLE ACCOUNT (
    AccountID       INT IDENTITY(1,1) PRIMARY KEY,
    Username        NVARCHAR(100)  NOT NULL,
    PasswordHash    NVARCHAR(500) NOT NULL,
    Email           NVARCHAR(256) NOT NULL,
    Phone           NVARCHAR(20)  NULL,
    FullName        NVARCHAR(100) NULL,
    Status          NVARCHAR(50)  NOT NULL DEFAULT N'Active',   -- Active | Inactive | Suspended
    Role            NVARCHAR(50)  NOT NULL DEFAULT N'Member',   -- Member | Staff | Admin
    OtpCode         NVARCHAR(6)   NULL,
    OtpExpiredAt    DATETIME2     NULL,
    OtpType         NVARCHAR(50)  NULL,                          -- Email | Phone
    CreatedAt       DATETIME2     NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE()),
    RefreshToken    NVARCHAR(500) NULL,
    RefreshExpiry   DATETIME2     NULL,
    IsTokenRevoked  BIT           NOT NULL DEFAULT 0
);
CREATE UNIQUE INDEX IX_ACCOUNT_Username ON ACCOUNT(Username);
CREATE UNIQUE INDEX IX_ACCOUNT_Email    ON ACCOUNT(Email);

CREATE TABLE MEMBER (
    MemberID       INT IDENTITY(1,1) PRIMARY KEY,
    AccountID      INT            NULL,
    FullName       NVARCHAR(200)  NOT NULL,
    FacePath       NVARCHAR(500)  NULL,
    FaceVector     NVARCHAR(MAX)  NULL,
    SpendingLimit  DECIMAL(18,2)  NULL,
    TotalPoints   INT            NOT NULL DEFAULT 0,
    CONSTRAINT FK_MEMBER_ACCOUNT FOREIGN KEY (AccountID)
        REFERENCES ACCOUNT(AccountID) ON DELETE SET NULL
);
CREATE INDEX IX_MEMBER_AccountID ON MEMBER(AccountID);

CREATE TABLE MEMBERSHIP (
    MembershipID   INT IDENTITY(1,1) PRIMARY KEY,
    MemberID       INT            NOT NULL,
    TierName       NVARCHAR(50)   NOT NULL,   -- Bronze | Silver | Gold | Platinum
    Status         NVARCHAR(50)   NOT NULL DEFAULT N'Active',  -- Active | Expired | Cancelled
    StartDate      DATETIME2      NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE()),
    EndDate        DATETIME2      NULL,
    CONSTRAINT FK_MEMBERSHIP_MEMBER FOREIGN KEY (MemberID)
        REFERENCES MEMBER(MemberID) ON DELETE CASCADE
);
CREATE INDEX IX_MEMBERSHIP_MemberID ON MEMBERSHIP(MemberID);

CREATE TABLE HEALTH_TAG (
    HealthTagID    INT IDENTITY(1,1) PRIMARY KEY,
    TagName        NVARCHAR(100) NOT NULL,
    TagType        NVARCHAR(50)  NOT NULL DEFAULT N'diet'  -- diet | allergy | ingredient | lifestyle
);

CREATE TABLE MEMBERHEALTH_PREFERENCE (
    MemberID     INT           NOT NULL,
    HealthTagID  INT           NOT NULL,
    Status       NVARCHAR(50)  NOT NULL,   -- Allergy | Avoid | Preferred
    CONSTRAINT PK_MEMBERHEALTH_PREFERENCE PRIMARY KEY (MemberID, HealthTagID),
    CONSTRAINT FK_MHP_MEMBER FOREIGN KEY (MemberID)
        REFERENCES MEMBER(MemberID) ON DELETE CASCADE,
    CONSTRAINT FK_MHP_HEALTHTAG FOREIGN KEY (HealthTagID)
        REFERENCES HEALTH_TAG(HealthTagID) ON DELETE CASCADE
);

-- ────────────────────────────────────────────────────────────────────────────
-- REGION 2: Product Catalog
-- ────────────────────────────────────────────────────────────────────────────

CREATE TABLE CATEGORY (
    CategoryID    INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL,
    CategoryIcon NVARCHAR(100) NULL
);

CREATE TABLE SUBCATEGORY (
    SubcategoryID   INT IDENTITY(1,1) PRIMARY KEY,
    CategoryID      INT            NOT NULL,
    SubcategoryName NVARCHAR(100)  NOT NULL,
    CONSTRAINT FK_SUBCATEGORY_CATEGORY FOREIGN KEY (CategoryID)
        REFERENCES CATEGORY(CategoryID) ON DELETE CASCADE
);
CREATE INDEX IX_SUBCATEGORY_CategoryID ON SUBCATEGORY(CategoryID);

CREATE TABLE PRODUCT_TYPE (
    ProductTypeID  INT IDENTITY(1,1) PRIMARY KEY,
    SubcategoryID  INT           NOT NULL,
    TypeName       NVARCHAR(100) NOT NULL,
    CONSTRAINT FK_PRODUCTTYPE_SUBCATEGORY FOREIGN KEY (SubcategoryID)
        REFERENCES SUBCATEGORY(SubcategoryID) ON DELETE CASCADE
);
CREATE INDEX IX_PRODUCTTYPE_SubcategoryID ON PRODUCT_TYPE(SubcategoryID);

CREATE TABLE PRODUCT (
    ProductID          INT IDENTITY(1,1) PRIMARY KEY,
    ProductTypeID      INT            NOT NULL,
    ProductName        NVARCHAR(200)  NOT NULL,
    UnitPrice          DECIMAL(18,2)  NOT NULL DEFAULT 0,
    PromotionPrice     DECIMAL(18,2)  NULL,
    ExpiredDate        DATE           NULL,
    ImageUrl           NVARCHAR(500)  NULL,
    WeightOrVolume     DECIMAL(18,3)  NULL,
    Unit               NVARCHAR(20)   NULL,
    Description        NVARCHAR(MAX)  NULL,
    Status             NVARCHAR(50)   NOT NULL DEFAULT N'Available',  -- Available | Discontinued | OutOfStock
    SubstituteProductID INT           NULL,
    CONSTRAINT FK_PRODUCT_TYPE FOREIGN KEY (ProductTypeID)
        REFERENCES PRODUCT_TYPE(ProductTypeID),
    CONSTRAINT FK_PRODUCT_SUBSTITUTE FOREIGN KEY (SubstituteProductID)
        REFERENCES PRODUCT(ProductID) ON DELETE SET NULL
);
CREATE INDEX IX_PRODUCT_ProductTypeID ON PRODUCT(ProductTypeID);
CREATE INDEX IX_PRODUCT_Status ON PRODUCT(Status);

CREATE TABLE PRODUCT_HEALTHTAG (
    ProductID    INT NOT NULL,
    HealthTagID  INT NOT NULL,
    CONSTRAINT PK_PRODUCT_HEALTHTAG PRIMARY KEY (ProductID, HealthTagID),
    CONSTRAINT FK_PHT_PRODUCT  FOREIGN KEY (ProductID)   REFERENCES PRODUCT(ProductID)   ON DELETE CASCADE,
    CONSTRAINT FK_PHT_TAG      FOREIGN KEY (HealthTagID) REFERENCES HEALTH_TAG(HealthTagID) ON DELETE CASCADE
);

-- ────────────────────────────────────────────────────────────────────────────
-- REGION 3: Shopping & Meal
-- ────────────────────────────────────────────────────────────────────────────

CREATE TABLE INVOICE_HISTORY (
    InvoiceHistoryID  INT IDENTITY(1,1) PRIMARY KEY,
    MemberID          INT            NOT NULL,
    PurchaseDate      DATETIME2      NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE()),
    TotalPrice        DECIMAL(18,2)  NOT NULL DEFAULT 0,
    PaymentMethod     NVARCHAR(50)   NULL,
    PointsEarned     INT            NOT NULL DEFAULT 0,
    CONSTRAINT FK_INVOICE_MEMBER FOREIGN KEY (MemberID) REFERENCES MEMBER(MemberID)
);
CREATE INDEX IX_INVOICE_MEMBER ON INVOICE_HISTORY(MemberID);

CREATE TABLE INVOICE_HISTORY_ITEM (
    InvoiceHistoryItemID  INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceHistoryID       INT           NOT NULL,
    ProductID              INT           NOT NULL,
    Quantity               INT           NOT NULL DEFAULT 1,
    UnitPrice              DECIMAL(18,2) NOT NULL,
    DiscountAmount         DECIMAL(18,2) NOT NULL DEFAULT 0,
    CONSTRAINT FK_IHI_INVOICE FOREIGN KEY (InvoiceHistoryID) REFERENCES INVOICE_HISTORY(InvoiceHistoryID) ON DELETE CASCADE,
    CONSTRAINT FK_IHI_PRODUCT  FOREIGN KEY (ProductID)       REFERENCES PRODUCT(ProductID)
);
CREATE INDEX IX_INVOICE_ITEM_InvoiceID ON INVOICE_HISTORY_ITEM(InvoiceHistoryID);
CREATE INDEX IX_INVOICE_ITEM_ProductID ON INVOICE_HISTORY_ITEM(ProductID);

CREATE TABLE MEAL_SUGGESTION (
    MealSuggestionID      INT IDENTITY(1,1) PRIMARY KEY,
    MealName              NVARCHAR(200)  NOT NULL,
    Description           NVARCHAR(MAX)  NULL,
    YieldPortions         INT            NOT NULL DEFAULT 1,
    ImageUrl              NVARCHAR(500)  NULL,
    Calories              INT            NULL,
    HealthyScore          DECIMAL(5,2)   NULL,
    AlternativeSuggestion NVARCHAR(500)  NULL
);

CREATE TABLE MEAL_ITEM (
    MealSuggestionID    INT             NOT NULL,
    ProductID           INT             NOT NULL,
    QuantityRequired    DECIMAL(18,3)   NOT NULL DEFAULT 1,
    UnitOfMeasure       NVARCHAR(20)   NULL,
    CONSTRAINT PK_MEAL_ITEM PRIMARY KEY (MealSuggestionID, ProductID),
    CONSTRAINT FK_MI_MEALSUGGESTION FOREIGN KEY (MealSuggestionID)
        REFERENCES MEAL_SUGGESTION(MealSuggestionID) ON DELETE CASCADE,
    CONSTRAINT FK_MI_PRODUCT FOREIGN KEY (ProductID) REFERENCES PRODUCT(ProductID)
);

-- ────────────────────────────────────────────────────────────────────────────
-- REGION 4: Store Layout (Floor → Zone → Aisle → Shelf → Slot)
-- ────────────────────────────────────────────────────────────────────────────

CREATE TABLE FLOOR (
    FloorID      INT IDENTITY(1,1) PRIMARY KEY,
    FloorNumber  INT NOT NULL DEFAULT 1,
    FloorName    NVARCHAR(100) NULL
);

CREATE TABLE ZONE (
    ZoneID       INT IDENTITY(1,1) PRIMARY KEY,
    FloorID      INT            NOT NULL,
    ZoneName     NVARCHAR(100)  NULL,
    Description  NVARCHAR(500)  NULL,
    XMin         FLOAT          NULL,   -- bounding box cho map
    YMin         FLOAT          NULL,
    XMax         FLOAT          NULL,
    YMax         FLOAT          NULL,
    CONSTRAINT FK_ZONE_FLOOR FOREIGN KEY (FloorID) REFERENCES FLOOR(FloorID) ON DELETE CASCADE
);
CREATE INDEX IX_ZONE_FloorID ON ZONE(FloorID);

CREATE TABLE AISLE (
    AisleID     INT IDENTITY(1,1) PRIMARY KEY,
    ZoneID      INT           NOT NULL,
    AisleCode   NVARCHAR(20)  NOT NULL,
    AisleName   NVARCHAR(100) NULL,
    StartNodeID INT           NULL,   -- NavigationNode đầu lối đi
    EndNodeID   INT           NULL,   -- NavigationNode cuối lối đi
    CONSTRAINT FK_AISLE_ZONE FOREIGN KEY (ZoneID) REFERENCES ZONE(ZoneID) ON DELETE CASCADE
);
CREATE INDEX IX_AISLE_ZoneID ON AISLE(ZoneID);

CREATE TABLE SHELF (
    ShelfID      INT IDENTITY(1,1) PRIMARY KEY,
    AisleID      INT NOT NULL,
    LevelNumber  INT NOT NULL DEFAULT 1,   -- 1=bottom, 2=mid, 3=top
    ShelfName    NVARCHAR(100) NULL,
    CONSTRAINT FK_SHELF_AISLE FOREIGN KEY (AisleID) REFERENCES AISLE(AisleID) ON DELETE CASCADE
);
CREATE INDEX IX_SHELF_AisleID ON SHELF(AisleID);

CREATE TABLE SLOT (
    SlotID         INT IDENTITY(1,1) PRIMARY KEY,
    ShelfID        INT            NOT NULL,
    SlotCode       NVARCHAR(20)   NULL,   -- ví dụ: "A1-L2-S3"
    Quantity       INT            NOT NULL DEFAULT 0,
    LastScannedAt  DATETIME2      NULL,
    CONSTRAINT FK_SLOT_SHELF FOREIGN KEY (ShelfID) REFERENCES SHELF(ShelfID) ON DELETE CASCADE
);
CREATE INDEX IX_SLOT_ShelfID ON SLOT(ShelfID);

-- Junction: nhiều sản phẩm có thể nằm trong 1 slot (khuyến mãi, bundle)
CREATE TABLE PRODUCT_SLOT (
    ProductsSlotID  INT IDENTITY(1,1) PRIMARY KEY,
    SlotID          INT NOT NULL,
    ProductID       INT NOT NULL,
    ProductQuantity INT NOT NULL DEFAULT 1,
    CONSTRAINT FK_PS_SLOT    FOREIGN KEY (SlotID)    REFERENCES SLOT(SlotID)    ON DELETE CASCADE,
    CONSTRAINT FK_PS_PRODUCT FOREIGN KEY (ProductID) REFERENCES PRODUCT(ProductID) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IX_PRODUCT_SLOT_slot_product ON PRODUCT_SLOT(SlotID, ProductID);

-- ────────────────────────────────────────────────────────────────────────────
-- REGION 5: Advertising & Sponsorship
-- ────────────────────────────────────────────────────────────────────────────

CREATE TABLE BRAND (
    BrandID     INT IDENTITY(1,1) PRIMARY KEY,
    BrandName   NVARCHAR(100) NOT NULL,
    Wallet      DECIMAL(18,2) NOT NULL DEFAULT 0,
    Description NVARCHAR(500) NULL,
    LogoUrl     NVARCHAR(500) NULL
);

CREATE TABLE AD_PACKAGE (
    PackageID      INT IDENTITY(1,1) PRIMARY KEY,
    PackageName    NVARCHAR(100)  NOT NULL,
    PricePackage   DECIMAL(18,2) NOT NULL DEFAULT 0,   -- giá gói cố định
    PriceRoute     DECIMAL(18,2) NOT NULL DEFAULT 0,   -- giá theo lộ trình
    BasePriceClick DECIMAL(18,2) NOT NULL DEFAULT 0,
    AdScore        INT           NOT NULL DEFAULT 0,
    Status         NVARCHAR(50)  NOT NULL DEFAULT N'Active'
);

CREATE TABLE ROBOT_ZONE (
    RobotZoneID  INT IDENTITY(1,1) PRIMARY KEY,
    RobotID      INT NOT NULL,
    ZoneID       INT NOT NULL,
    AssignedAt   DATETIME2 NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE()),
    CONSTRAINT FK_RZ_ROBOT FOREIGN KEY (RobotID) REFERENCES ROBOT(RobotID) ON DELETE CASCADE,
    CONSTRAINT FK_RZ_ZONE  FOREIGN KEY (ZoneID)  REFERENCES ZONE(ZoneID)  ON DELETE CASCADE
);
CREATE UNIQUE INDEX IX_ROBOT_ZONE_Robot ON ROBOT_ZONE(RobotID);  -- 1 robot 1 zone

CREATE TABLE AD_CAMPAIGN (
    AdCampaignID   INT IDENTITY(1,1) PRIMARY KEY,
    PackageID      INT           NOT NULL,
    BrandID        INT           NOT NULL,
    RobotZoneID    INT           NULL,
    CampaignName   NVARCHAR(200) NOT NULL,
    StartDate      DATETIME2     NOT NULL,
    EndDate        DATETIME2     NOT NULL,
    Status         NVARCHAR(50)  NOT NULL DEFAULT N'Active',
    Budget         DECIMAL(18,2) NOT NULL DEFAULT 0,
    Spent          DECIMAL(18,2) NOT NULL DEFAULT 0,
    CONSTRAINT FK_ADC_PACKAGE    FOREIGN KEY (PackageID)   REFERENCES AD_PACKAGE(PackageID),
    CONSTRAINT FK_ADC_BRAND      FOREIGN KEY (BrandID)     REFERENCES BRAND(BrandID),
    CONSTRAINT FK_ADC_ROBOTZONE  FOREIGN KEY (RobotZoneID) REFERENCES ROBOT_ZONE(RobotZoneID) ON DELETE SET NULL
);
CREATE INDEX IX_ADC_Status_Dates ON AD_CAMPAIGN(Status, StartDate, EndDate);

CREATE TABLE SPONSORED_PRODUCT (
    SponsoredID    INT IDENTITY(1,1) PRIMARY KEY,
    AdCampaignID   INT           NOT NULL,
    ProductID      INT           NOT NULL,
    Priority       INT           NOT NULL DEFAULT 0,
    Status         NVARCHAR(50)  NOT NULL DEFAULT N'Active',
    CONSTRAINT FK_SP_ADCAMPAIGN FOREIGN KEY (AdCampaignID) REFERENCES AD_CAMPAIGN(AdCampaignID) ON DELETE CASCADE,
    CONSTRAINT FK_SP_PRODUCT    FOREIGN KEY (ProductID)    REFERENCES PRODUCT(ProductID) ON DELETE CASCADE
);
CREATE INDEX IX_SP_AdCampaignID ON SPONSORED_PRODUCT(AdCampaignID);

CREATE TABLE AD_RESOURCE (
    ResourceID    INT IDENTITY(1,1) PRIMARY KEY,
    AdCampaignID  INT           NOT NULL,
    ResourceType  NVARCHAR(20) NOT NULL,   -- image | video | text
    ResourceURL   NVARCHAR(500) NOT NULL,
    ContentText   NVARCHAR(MAX) NULL,
    Resolution    NVARCHAR(20) NULL,
    Status        NVARCHAR(50) NOT NULL DEFAULT N'Active',
    CONSTRAINT FK_AR_ADCAMPAIGN FOREIGN KEY (AdCampaignID) REFERENCES AD_CAMPAIGN(AdCampaignID) ON DELETE CASCADE
);

CREATE TABLE AD_CAMPAIGN_LOG (
    LogID          INT IDENTITY(1,1) PRIMARY KEY,
    AdCampaignID   INT           NOT NULL,
    ActionType     NVARCHAR(50)  NOT NULL,   -- RoutePass | Click | View
    ChargedAmount  DECIMAL(18,2) NOT NULL DEFAULT 0,
    Timestamp      DATETIME2     NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE()),
    -- RoutePass phase B: các cột nullable cho dữ liệu cũ vẫn lưu được
    SponsoredID    INT           NULL,
    ProductID      INT           NULL,
    RobotID        INT           NULL,
    RobotZoneID    INT           NULL,
    ZoneID         INT           NULL,
    SlotID         INT           NULL,
    MemberID       INT           NULL,
    SessionID      NVARCHAR(100) NULL,
    XCoord         FLOAT         NULL,
    YCoord         FLOAT         NULL,
    CONSTRAINT FK_ACL_ADCAMPAIGN FOREIGN KEY (AdCampaignID) REFERENCES AD_CAMPAIGN(AdCampaignID) ON DELETE CASCADE,
    CONSTRAINT FK_ACL_SPONSORED  FOREIGN KEY (SponsoredID)   REFERENCES SPONSORED_PRODUCT(SponsoredID) ON DELETE SET NULL,
    CONSTRAINT FK_ACL_PRODUCT    FOREIGN KEY (ProductID)     REFERENCES PRODUCT(ProductID),
    CONSTRAINT FK_ACL_ROBOT      FOREIGN KEY (RobotID)       REFERENCES ROBOT(RobotID) ON DELETE SET NULL,
    CONSTRAINT FK_ACL_ROBOTZONE  FOREIGN KEY (RobotZoneID)   REFERENCES ROBOT_ZONE(RobotZoneID) ON DELETE SET NULL,
    CONSTRAINT FK_ACL_ZONE       FOREIGN KEY (ZoneID)        REFERENCES ZONE(ZoneID),
    CONSTRAINT FK_ACL_SLOT       FOREIGN KEY (SlotID)        REFERENCES SLOT(SlotID),
    CONSTRAINT FK_ACL_MEMBER     FOREIGN KEY (MemberID)      REFERENCES MEMBER(MemberID) ON DELETE SET NULL
);
CREATE INDEX IX_ACL_AdCampaignID ON AD_CAMPAIGN_LOG(AdCampaignID);
CREATE INDEX IX_ACL_Timestamp    ON AD_CAMPAIGN_LOG(Timestamp);

-- ────────────────────────────────────────────────────────────────────────────
-- REGION 6: Robot & Navigation
-- ────────────────────────────────────────────────────────────────────────────

CREATE TABLE ROBOT (
    RobotID      INT IDENTITY(1,1) PRIMARY KEY,
    RobotName    NVARCHAR(100) NOT NULL,
    RobotCode    NVARCHAR(50)  NOT NULL,
    BatteryPct   INT           NOT NULL DEFAULT 100,
    Mode         NVARCHAR(50)  NOT NULL DEFAULT N'idle',    -- idle | navigating | scanning | charging | returning
    Status       NVARCHAR(50)  NOT NULL DEFAULT N'Offline', -- Online | Offline | Maintenance | Error
    LastSeenAt   DATETIME2     NULL,
    IPAddress    NVARCHAR(45)  NULL,
    FirmwareVer  NVARCHAR(20)  NULL,
    CurrentX     FLOAT         NULL,   -- tọa độ hiện tại
    CurrentY     FLOAT         NULL,
    CurrentHeading FLOAT       NULL    -- radian
);
CREATE UNIQUE INDEX IX_ROBOT_RobotCode ON ROBOT(RobotCode);

CREATE TABLE ROBOT_LOG (
    LogID       INT IDENTITY(1,1) PRIMARY KEY,
    RobotID     INT           NULL,
    Battery     INT           NULL,
    Location    NVARCHAR(200) NULL,
    Status      NVARCHAR(50)  NOT NULL DEFAULT N'Idle',
    Timestamp   DATETIME2     NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE()),
    XCoord      FLOAT         NULL,
    YCoord      FLOAT         NULL,
    HeadingRad  FLOAT         NULL,
    ErrorCode   NVARCHAR(50)  NULL,
    Message     NVARCHAR(MAX) NULL,
    CONSTRAINT FK_RL_ROBOT FOREIGN KEY (RobotID) REFERENCES ROBOT(RobotID) ON DELETE SET NULL
);
CREATE INDEX IX_ROBOT_LOG_robot_timestamp ON ROBOT_LOG(RobotID, Timestamp DESC);

CREATE TABLE MAP (
    MapID      INT IDENTITY(1,1) PRIMARY KEY,
    FloorID    INT           NOT NULL,
    MapName    NVARCHAR(100) NOT NULL,
    MapData    NVARCHAR(MAX) NULL,   -- JSON: floorplan image URL, grid metadata
    ImageUrl   NVARCHAR(500) NULL,   -- URL ảnh bản đồ tĩnh
    GridWidth  INT           NULL,    -- số ô theo X
    GridHeight INT           NULL,    -- số ô theo Y
    CellSize   FLOAT         NULL,   -- kích thước 1 ô (m)
    CreatedAt  DATETIME2     NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE()),
    UpdatedAt  DATETIME2     NULL,
    CONSTRAINT FK_MAP_FLOOR FOREIGN KEY (FloorID) REFERENCES FLOOR(FloorID) ON DELETE CASCADE
);
CREATE INDEX IX_MAP_FloorID ON MAP(FloorID);

-- Điểm neo đồ thị đồ thị (Waypoint)
CREATE TABLE NAVIGATION_NODE (
    NodeID     INT IDENTITY(1,1) PRIMARY KEY,
    MapID      INT           NOT NULL,
    NodeName   NVARCHAR(100) NULL,   -- ví dụ: "N_A1_START", "N_CROSS_02"
    XCoord     FLOAT         NOT NULL DEFAULT 0,
    YCoord     FLOAT         NOT NULL DEFAULT 0,
    NodeType   NVARCHAR(50)  NOT NULL DEFAULT N'intersection',  -- intersection | aisle | entrance | exit | charging | shelf
    IsBlocked  BIT           NOT NULL DEFAULT 0,    -- tạm khóa (vùng cấm / robot đang đứng)
    IsVirtual  BIT           NOT NULL DEFAULT 0,    -- node ảo cho tính toán đường đi
    CONSTRAINT FK_NN_MAP FOREIGN KEY (MapID) REFERENCES MAP(MapID) ON DELETE CASCADE
);
CREATE INDEX IX_NN_MapID       ON NAVIGATION_NODE(MapID);
CREATE INDEX IX_NN_IsBlocked  ON NAVIGATION_NODE(IsBlocked);

-- Cạnh đồ thị (liên kết 2 waypoint)
CREATE TABLE NAVIGATION_EDGE (
    EdgeID           INT IDENTITY(1,1) PRIMARY KEY,
    FromNodeID       INT  NOT NULL,
    ToNodeID         INT  NOT NULL,
    Distance         FLOAT NOT NULL DEFAULT 0,   -- mét
    IsBidirectional  BIT  NOT NULL DEFAULT 1,
    CostMultiplier   FLOAT NOT NULL DEFAULT 1.0, -- trọng số (vùng đông → cost cao)
    IsBlocked        BIT  NOT NULL DEFAULT 0,
    CONSTRAINT FK_NE_FROM FOREIGN KEY (FromNodeID) REFERENCES NAVIGATION_NODE(NodeID),
    CONSTRAINT FK_NE_TO   FOREIGN KEY (ToNodeID)   REFERENCES NAVIGATION_NODE(NodeID)
);
CREATE INDEX IX_NE_FromNodeID ON NAVIGATION_EDGE(FromNodeID);
CREATE INDEX IX_NE_ToNodeID   ON NAVIGATION_EDGE(ToNodeID);

-- Map aisle → start/end navigation node
CREATE TABLE AISLE_NODE (
    AisleNodeID  INT IDENTITY(1,1) PRIMARY KEY,
    AisleID      INT NOT NULL,
    NodeID       INT NOT NULL,
    IsStart      BIT NOT NULL DEFAULT 1,   -- 1=đầu lối đi, 0=cuối lối đi
    CONSTRAINT FK_AN_AISLE FOREIGN KEY (AisleID) REFERENCES AISLE(AisleID) ON DELETE CASCADE,
    CONSTRAINT FK_AN_NODE  FOREIGN KEY (NodeID)  REFERENCES NAVIGATION_NODE(NodeID) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IX_AISLE_NODE_Aisle_Start ON AISLE_NODE(AisleID, IsStart);

-- Lộ trình cố định (ví dụ: patrol route)
CREATE TABLE ROBOT_ROUTE (
    RobotRouteID  INT IDENTITY(1,1) PRIMARY KEY,
    RobotID       INT           NOT NULL,
    MapID         INT           NOT NULL,
    RouteName     NVARCHAR(200) NULL,
    RouteType     NVARCHAR(50)  NOT NULL DEFAULT N'patrol',  -- patrol | restock | delivery | custom
    IsActive      BIT           NOT NULL DEFAULT 1,
    CreatedAt     DATETIME2     NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE()),
    CONSTRAINT FK_RR_ROBOT FOREIGN KEY (RobotID) REFERENCES ROBOT(RobotID) ON DELETE CASCADE,
    CONSTRAINT FK_RR_MAP   FOREIGN KEY (MapID)   REFERENCES MAP(MapID)
);
CREATE INDEX IX_RR_RobotID ON ROBOT_ROUTE(RobotID);

-- Thứ tự các node trong 1 route
CREATE TABLE ROUTE_NODE_MAPPING (
    RouteNodeMappingID  INT IDENTITY(1,1) PRIMARY KEY,
    RobotRouteID        INT NOT NULL,
    NodeID              INT NOT NULL,
    SequenceOrder       INT NOT NULL DEFAULT 0,
    WaitTimeSec         INT NOT NULL DEFAULT 0,   -- thời gian chờ tại node (scan shelf)
    Instruction         NVARCHAR(MAX) NULL,       -- lệnh đặc biệt: TURN_LEFT | SCAN | LIFT | ...
    CONSTRAINT FK_RNM_ROUTE FOREIGN KEY (RobotRouteID) REFERENCES ROBOT_ROUTE(RobotRouteID) ON DELETE CASCADE,
    CONSTRAINT FK_RNM_NODE  FOREIGN KEY (NodeID)       REFERENCES NAVIGATION_NODE(NodeID)
);
CREATE INDEX IX_RNM_RouteID ON ROUTE_NODE_MAPPING(RobotRouteID);

-- Gán route cho robot để thực thi
CREATE TABLE ROUTE_ASSIGNMENT (
    RouteAssignmentID  INT IDENTITY(1,1) PRIMARY KEY,
    RobotID            INT           NOT NULL,
    RobotRouteID       INT           NOT NULL,
    AssignedAt         DATETIME2     NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE()),
    Status             NVARCHAR(50)  NOT NULL DEFAULT N'Pending',  -- Pending | InProgress | Completed | Cancelled | Failed
    StartedAt          DATETIME2     NULL,
    CompletedAt        DATETIME2     NULL,
    Notes              NVARCHAR(MAX) NULL,
    CONSTRAINT FK_RA_ROBOT FOREIGN KEY (RobotID)     REFERENCES ROBOT(RobotID)     ON DELETE CASCADE,
    CONSTRAINT FK_RA_ROUTE FOREIGN KEY (RobotRouteID) REFERENCES ROBOT_ROUTE(RobotRouteID)
);
CREATE INDEX IX_RA_RobotID    ON ROUTE_ASSIGNMENT(RobotID);
CREATE INDEX IX_RA_RouteID    ON ROUTE_ASSIGNMENT(RobotRouteID);
CREATE INDEX IX_RA_Status     ON ROUTE_ASSIGNMENT(Status);

-- Lưu ảnh kệ hàng khi robot scan
CREATE TABLE AISLE_SCAN (
    ScanID           INT IDENTITY(1,1) PRIMARY KEY,
    AisleID          INT           NOT NULL,
    RobotID          INT           NOT NULL,
    ScannedAt        DATETIME2     NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE()),
    EmptyPercentage  DECIMAL(5,2)  NOT NULL DEFAULT 0,
    NeedsRestock     BIT           NOT NULL DEFAULT 0,
    ImageUrl         NVARCHAR(500) NULL,
    CameraAngle      FLOAT         NULL,  -- góc camera khi chụp (rad)
    XCoord           FLOAT         NULL,
    YCoord           FLOAT         NULL,
    CONSTRAINT FK_AS_AISLE FOREIGN KEY (AisleID) REFERENCES AISLE(AisleID),
    CONSTRAINT FK_AS_ROBOT  FOREIGN KEY (RobotID)  REFERENCES ROBOT(RobotID)
);
CREATE INDEX IX_AS_AisleID  ON AISLE_SCAN(AisleID);
CREATE INDEX IX_AS_RobotID  ON AISLE_SCAN(RobotID);
CREATE INDEX IX_AS_ScannedAt ON AISLE_SCAN(ScannedAt);

-- Đối tượng ngữ nghĩa phát hiện trên bản đồ (shelf, entrance, obstacle...)
CREATE TABLE SEMANTIC_OBJECT (
    ObjectID     INT IDENTITY(1,1) PRIMARY KEY,
    MapID        INT           NOT NULL,
    ObjectType   NVARCHAR(100) NOT NULL,  -- shelf | obstacle | entrance | exit | charging_station | person | cart
    XMin         FLOAT         NOT NULL DEFAULT 0,
    YMin         FLOAT         NOT NULL DEFAULT 0,
    XMax         FLOAT         NOT NULL DEFAULT 0,
    YMax         FLOAT         NOT NULL DEFAULT 0,
    Label        NVARCHAR(100) NULL,
    Confidence   FLOAT         NULL,
    DetectedAt   DATETIME2     NULL,
    ImageUrl     NVARCHAR(500) NULL,
    IsVerified   BIT           NOT NULL DEFAULT 0,  -- staff xác nhận
    CONSTRAINT FK_SO_MAP FOREIGN KEY (MapID) REFERENCES MAP(MapID) ON DELETE CASCADE
);
CREATE INDEX IX_SO_MapID ON SEMANTIC_OBJECT(MapID);
CREATE INDEX IX_SO_ObjectType ON SEMANTIC_OBJECT(ObjectType);

-- CART & CART_ITEM (Shopping Cart Option A)
CREATE TABLE CART (
    CartID       INT IDENTITY(1,1) PRIMARY KEY,
    MemberID     INT            NOT NULL UNIQUE,
    CreatedAt    DATETIME2      NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE()),
    UpdatedAt    DATETIME2      NULL,
    CONSTRAINT FK_CART_MEMBER FOREIGN KEY (MemberID) REFERENCES MEMBER(MemberID) ON DELETE CASCADE
);
CREATE INDEX IX_CART_MemberID ON CART(MemberID);

CREATE TABLE CART_ITEM (
    CartItemID   INT IDENTITY(1,1) PRIMARY KEY,
    CartID       INT            NOT NULL,
    ProductID    INT            NOT NULL,
    Quantity     INT            NOT NULL DEFAULT 1,
    AddedAt      DATETIME2      NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE()),
    CONSTRAINT FK_CI_CART FOREIGN KEY (CartID) REFERENCES CART(CartID) ON DELETE CASCADE,
    CONSTRAINT FK_CI_PRODUCT FOREIGN KEY (ProductID) REFERENCES PRODUCT(ProductID) ON DELETE CASCADE
);
CREATE INDEX IX_CART_ITEM_CartID ON CART_ITEM(CartID);
CREATE INDEX IX_CART_ITEM_ProductID ON CART_ITEM(ProductID);

