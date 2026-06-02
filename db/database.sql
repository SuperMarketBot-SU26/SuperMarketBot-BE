-- ============================================================================
-- SMARTMARKETBOT - SYSTEM DATABASE SCRIPT (SQL SERVER 2022+)
-- Merge Schema cho Cả Backend .NET 10 (Clean Architecture) & AI FastAPI / n8n Workflow
-- Bản Hoàn Chỉnh, Idempotent (Chạy lại an toàn), Đầy Đủ Seed Data Siêu Thị Việt Nam
-- Sửa lỗi Msg 1785 (Vòng lặp Cascade) & Tối ưu hóa lô xử lý bằng lệnh GO độc lập
-- ============================================================================

IF DB_ID(N'SuperMarketBot') IS NULL
BEGIN
    CREATE DATABASE SuperMarketBot;
END
GO

USE SuperMarketBot;
GO

-- ============================================================================
-- PART 1: DỌN DẸP VIEW VÀ TABLE NẾU TỒN TẠI (Đảm bảo chạy lại script an toàn)
-- ============================================================================

-- 1. Dọn dẹp Views
DROP VIEW IF EXISTS dbo.Real_Time_Stock;
GO
DROP VIEW IF EXISTS dbo.Blocked_Aisles;
GO
DROP VIEW IF EXISTS dbo.Store_Map;
GO
DROP VIEW IF EXISTS dbo.PurchaseHistory;
GO

-- 2. Dọn dẹp Tables (Xóa theo thứ tự ngược chiều quan hệ khoá ngoại)
DROP TABLE IF EXISTS dbo.MemberEvents;
GO
DROP TABLE IF EXISTS dbo.MemberAlerts;
GO
DROP TABLE IF EXISTS dbo.ForbiddenZones;
GO
DROP TABLE IF EXISTS dbo.SponsoredProducts;
GO
DROP TABLE IF EXISTS dbo.PromotionProducts;
GO
DROP TABLE IF EXISTS dbo.Promotions;
GO
DROP TABLE IF EXISTS dbo.RecipeItems;
GO
DROP TABLE IF EXISTS dbo.Recipes;
GO
DROP TABLE IF EXISTS dbo.HistoryItems;
GO
DROP TABLE IF EXISTS dbo.ShoppingHistories;
GO
DROP TABLE IF EXISTS dbo.ShelfScans;
GO
DROP TABLE IF EXISTS dbo.Robot_Logs;
GO
DROP TABLE IF EXISTS dbo.SemanticObjects;
GO
DROP TABLE IF EXISTS dbo.Workstations;
GO
DROP TABLE IF EXISTS dbo.NavigationEdges;
GO
DROP TABLE IF EXISTS dbo.NavigationNodes;
GO
DROP TABLE IF EXISTS dbo.Maps;
GO
DROP TABLE IF EXISTS dbo.RobotZones;
GO
DROP TABLE IF EXISTS dbo.Robots;
GO
DROP TABLE IF EXISTS dbo.MemberHealthPreferences;
GO
DROP TABLE IF EXISTS dbo.ProductHealthTags;
GO
DROP TABLE IF EXISTS dbo.HealthTags;
GO
DROP TABLE IF EXISTS dbo.Slots;
GO
DROP TABLE IF EXISTS dbo.Products;
GO
DROP TABLE IF EXISTS dbo.ProductTypes;
GO
DROP TABLE IF EXISTS dbo.Subcategories;
GO
DROP TABLE IF EXISTS dbo.Categories;
GO
DROP TABLE IF EXISTS dbo.ShelfLevels;
GO
DROP TABLE IF EXISTS dbo.Aisles;
GO
DROP TABLE IF EXISTS dbo.Zones;
GO
DROP TABLE IF EXISTS dbo.Floors;
GO
DROP TABLE IF EXISTS dbo.Supermarkets;
GO
DROP TABLE IF EXISTS dbo.Staff;
GO
DROP TABLE IF EXISTS dbo.Admins;
GO
DROP TABLE IF EXISTS dbo.Members;
GO
DROP TABLE IF EXISTS dbo.UserRoles;
GO
DROP TABLE IF EXISTS dbo.Roles;
GO
DROP TABLE IF EXISTS dbo.Users;
GO

-- ============================================================================
-- PART 2: ĐỊNH NGHĨA HỆ THỐNG BẢNG (38 TABLES) - CÓ GO PHÂN TÁCH LÔ XỬ LÝ
-- ============================================================================

-- 2.1. Phân hệ AUTH & USERS (5 Tables)

-- 1. Users: Tài khoản đăng nhập chung của hệ thống (Admin, Staff, Member)
CREATE TABLE dbo.Users (
    UserID        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Username      NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash  NVARCHAR(500) NOT NULL,
    Email         NVARCHAR(200) NULL,
    Phone         NVARCHAR(20) NULL,
    IsActive      BIT NOT NULL DEFAULT 1,
    CreatedAt     DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

-- 2. Roles: Vai trò hệ thống ('Admin', 'Staff', 'Member')
CREATE TABLE dbo.Roles (
    RoleID      INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    RoleName    NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(200) NULL
);
GO

-- 3. UserRoles: Bảng liên kết nhiều-nhiều giữa User và Role
CREATE TABLE dbo.UserRoles (
    UserID INT NOT NULL FOREIGN KEY REFERENCES dbo.Users(UserID) ON DELETE CASCADE,
    RoleID INT NOT NULL FOREIGN KEY REFERENCES dbo.Roles(RoleID) ON DELETE CASCADE,
    PRIMARY KEY (UserID, RoleID)
);
GO

-- 4. Members: Hồ sơ khách hàng hội viên (Merge hoàn hảo với bảng Members của AI)
-- Giữ nguyên MemberID, FullName, PhoneNumber, FacePath, FaceVector và MemberName (computed) để tương thích ngược 100% với AI FastAPI + n8n
CREATE TABLE dbo.Members (
    MemberID        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserID          INT NULL FOREIGN KEY REFERENCES dbo.Users(UserID) ON DELETE SET NULL, -- Nullable đề phòng đăng ký nhanh qua Face Login trước khi lập tài khoản web
    FullName        NVARCHAR(255) NOT NULL,
    PhoneNumber     VARCHAR(20) NOT NULL,
    FacePath        NVARCHAR(500) NULL,
    FaceVector      NVARCHAR(MAX) NULL, -- JSON lưu mảng nhúng khuôn mặt 128 chiều
    MemberName      AS (FullName),      -- Cột computed giữ độ tương thích ngược với câu query cũ của n8n/FastAPI
    Tier            NVARCHAR(20) NOT NULL DEFAULT 'Bronze', -- 'Bronze', 'Silver', 'Gold', 'Platinum'
    TotalPoints     INT NOT NULL DEFAULT 0,
    Avatar          NVARCHAR(500) NULL,
    TierUpdatedAt   DATETIME2 NULL,
    -- Buổi 16: Cá nhân hoá chế độ mua sắm & ngân sách stop-loss
    SearchMode      NVARCHAR(20) NOT NULL DEFAULT 'Normal', -- 'Normal', 'Healthy', 'Budget'
    ShoppingBudget  DECIMAL(12,2) NULL  -- Hạn mức ngân sách phiên mua sắm (NULL = không giới hạn)
);
GO

-- 5. Admins: Hồ sơ quản trị viên
CREATE TABLE dbo.Admins (
    AdminID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserID  INT NOT NULL FOREIGN KEY REFERENCES dbo.Users(UserID) ON DELETE CASCADE
);
GO

-- 2.2. Phân hệ STAFF (1 Table)

-- 6. Staff: Hồ sơ nhân viên siêu thị (Nhận cảnh báo hết hàng, xử lý robot)
CREATE TABLE dbo.Staff (
    StaffID   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserID    INT NOT NULL FOREIGN KEY REFERENCES dbo.Users(UserID) ON DELETE CASCADE,
    FirstName NVARCHAR(100) NULL,
    LastName  NVARCHAR(100) NULL,
    Phone     NVARCHAR(20) NULL,
    Email     NVARCHAR(200) NULL
);
GO

-- 2.3. Phân hệ STORE STRUCTURE (6 Tables)

-- 7. Supermarkets: Thông tin siêu thị
CREATE TABLE dbo.Supermarkets (
    SupermarketID   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    SupermarketName NVARCHAR(200) NOT NULL,
    Address         NVARCHAR(500) NULL,
    Status          NVARCHAR(50) NOT NULL DEFAULT 'Active'
);
GO

-- 8. Floors: Tầng lầu trong siêu thị
CREATE TABLE dbo.Floors (
    FloorID       INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    SupermarketID INT NOT NULL FOREIGN KEY REFERENCES dbo.Supermarkets(SupermarketID), -- Bỏ CASCADE để tránh Msg 1785
    FloorNumber   INT NOT NULL
);
GO

-- 9. Zones: Các phân khu mua sắm (VD: Phân khu A - Thực phẩm tươi sống)
CREATE TABLE dbo.Zones (
    ZoneID      INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    FloorID     INT NOT NULL FOREIGN KEY REFERENCES dbo.Floors(FloorID), -- Bỏ CASCADE để tránh Msg 1785
    ZoneCode    CHAR(1) NOT NULL, -- 'A', 'B', 'C', 'D', 'E'
    ZoneName    NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    IsBlocked   BIT NOT NULL DEFAULT 0 -- Vùng cấm robot đi vào
);
GO

-- 10. Aisles: Các dãy hàng nằm trong phân khu
-- Merge khái niệm bảng Blocked_Aisles của AI vào thẳng cấu trúc thực tế của dãy hàng
CREATE TABLE dbo.Aisles (
    AisleID     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ZoneID      INT NOT NULL FOREIGN KEY REFERENCES dbo.Zones(ZoneID), -- Bỏ CASCADE để tránh Msg 1785
    AisleCode   NVARCHAR(10) NOT NULL, -- 'A1', 'A2', 'B1', 'B2'
    AisleName   NVARCHAR(100) NULL,
    IsBlocked   BIT NOT NULL DEFAULT 0, -- Tương thích ngược với Blocked_Aisles của AI
    BlockReason NVARCHAR(255) NULL       -- Lí do chặn dãy hàng
);
GO

-- 11. ShelfLevels: Các tầng kệ của một dãy hàng (VD: Dãy A1 có tầng 1, 2, 3)
CREATE TABLE dbo.ShelfLevels (
    ShelfLevelID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    AisleID      INT NOT NULL FOREIGN KEY REFERENCES dbo.Aisles(AisleID), -- Bỏ CASCADE để tránh Msg 1785
    LevelNumber  INT NOT NULL -- 1: Tầng sát đất, 3: Tầm mắt, 5: Tầng trên cùng
);
GO

-- 12. Slots: Các ô trưng bày sản phẩm trên một tầng kệ (VD: Ô A1-T3-01)
CREATE TABLE dbo.Slots (
    SlotID        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ShelfLevelID  INT NOT NULL FOREIGN KEY REFERENCES dbo.ShelfLevels(ShelfLevelID), -- Bỏ CASCADE để tránh Msg 1785
    SlotCode      NVARCHAR(10) NOT NULL, -- '01', '02', '03'
    ProductID     INT NULL, -- Khai báo FOREIGN KEY ở sau khi có bảng Products
    Quantity      INT NOT NULL DEFAULT 0,
    LastScannedAt DATETIME2 NULL,
    -- Buổi 13: Quản lý kho chi tiết tại ô trưng bày
    ExpiryDate    DATE NULL,             -- Hạn sử dụng lô hàng đang đặt trong ô
    Supplier      NVARCHAR(200) NULL     -- Nhà cung cấp sản phẩm cụ thể
);
GO

-- 2.4. Phân hệ PRODUCT CATALOG (4 Tables)

-- 13. Categories: Danh mục sản phẩm cấp 1 (VD: Thực phẩm, Đồ uống, Hóa mỹ phẩm)
CREATE TABLE dbo.Categories (
    CategoryID   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL,
    Description  NVARCHAR(500) NULL
);
GO

-- 14. Subcategories: Danh mục sản phẩm cấp 2 (VD: Nước giải khát, Sữa tươi)
CREATE TABLE dbo.Subcategories (
    SubcategoryID   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    CategoryID      INT NOT NULL FOREIGN KEY REFERENCES dbo.Categories(CategoryID), -- Bỏ CASCADE để tránh Msg 1785
    SubcategoryName NVARCHAR(100) NOT NULL
);
GO

-- 15. ProductTypes: Loại sản phẩm cấp 3 (VD: Sữa tươi tiệt trùng, Nước ngọt có ga)
CREATE TABLE dbo.ProductTypes (
    ProductTypeID   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    SubcategoryID   INT NOT NULL FOREIGN KEY REFERENCES dbo.Subcategories(SubcategoryID), -- Bỏ CASCADE để tránh Msg 1785
    ProductTypeName NVARCHAR(100) NOT NULL
);
GO

-- 16. Products: Danh sách sản phẩm cụ thể (Cấp 4)
-- Tích hợp thuộc tính thay thế SubstituteProductID để phục vụ tính năng gợi ý khi hết hàng
CREATE TABLE dbo.Products (
    ProductID          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ProductTypeID      INT NOT NULL FOREIGN KEY REFERENCES dbo.ProductTypes(ProductTypeID), -- Bỏ CASCADE
    ProductName        NVARCHAR(200) NOT NULL,
    UnitPrice          DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    Barcode            NVARCHAR(50) NULL,
    ImageUrl           NVARCHAR(500) NULL,
    WeightOrVolume     DECIMAL(10,2) NULL,
    Unit               NVARCHAR(20) NULL, -- 'g', 'ml', 'box', 'item'
    Description        NVARCHAR(1000) NULL,
    IsActive           BIT NOT NULL DEFAULT 1,
    SubstituteProductID INT NULL FOREIGN KEY REFERENCES dbo.Products(ProductID) ON DELETE NO ACTION
);
GO

-- Thêm khoá ngoại giữa Slots và Products sau khi cả hai bảng đã được khởi tạo
ALTER TABLE dbo.Slots
ADD CONSTRAINT FK_Slots_Products FOREIGN KEY (ProductID) REFERENCES dbo.Products(ProductID) ON DELETE SET NULL;
GO

-- 2.5. Phân hệ HEALTH TAGS & ALLERGY (3 Tables)

-- 17. HealthTags: Nhãn sức khoẻ / dinh dưỡng (VD: Không đường, Vegan, Dị ứng lạc)
CREATE TABLE dbo.HealthTags (
    TagID   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    TagName NVARCHAR(50) NOT NULL,
    TagType NVARCHAR(20) NOT NULL -- 'diet' (chế độ ăn), 'allergy' (dị ứng), 'attribute' (thuộc tính)
);
GO

-- 18. ProductHealthTags: Gắn nhãn dinh dưỡng cho sản phẩm
CREATE TABLE dbo.ProductHealthTags (
    ProductID INT NOT NULL FOREIGN KEY REFERENCES dbo.Products(ProductID) ON DELETE CASCADE,
    TagID     INT NOT NULL FOREIGN KEY REFERENCES dbo.HealthTags(TagID) ON DELETE CASCADE,
    PRIMARY KEY (ProductID, TagID)
);
GO

-- 19. MemberHealthPreferences: Lưu cấu hình sức khoẻ/dị ứng của khách hàng
CREATE TABLE dbo.MemberHealthPreferences (
    MemberID  INT NOT NULL FOREIGN KEY REFERENCES dbo.Members(MemberID) ON DELETE CASCADE,
    TagID     INT NOT NULL FOREIGN KEY REFERENCES dbo.HealthTags(TagID) ON DELETE CASCADE,
    IsAllergy BIT NOT NULL DEFAULT 0, -- 1 = Bị dị ứng (Hệ thống sẽ quét check và cảnh báo nếu Hùng mua hạt), 0 = Sở thích cá nhân
    PRIMARY KEY (MemberID, TagID)
);
GO

-- 2.6. Phân hệ ROBOT & NAVIGATION (7 Tables)

-- 20. Robots: Quản lý hạm đội robot di động tự hành trong siêu thị
CREATE TABLE dbo.Robots (
    RobotID     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    RobotName   NVARCHAR(100) NOT NULL,
    RobotCode   NVARCHAR(50) NOT NULL UNIQUE,
    MacAddress  NVARCHAR(50) NULL,
    BatteryPct    INT NOT NULL DEFAULT 100,
    Mode          NVARCHAR(20) NOT NULL DEFAULT 'idle', -- 'idle', 'navigating', 'scanning', 'charging'
    IsOnline      BIT NOT NULL DEFAULT 0,
    LastSeenAt    DATETIME2 NULL,
    CurrentNodeID INT NULL -- Node hiện tại của robot (Phase 2 — cập nhật từ MQTT telemetry)
);
GO

-- 21. RobotZones: Phân công khu vực hoạt động cho Robot
CREATE TABLE dbo.RobotZones (
    RobotID INT NOT NULL FOREIGN KEY REFERENCES dbo.Robots(RobotID) ON DELETE CASCADE,
    ZoneID  INT NOT NULL FOREIGN KEY REFERENCES dbo.Zones(ZoneID), -- Sử dụng NO ACTION mặc định để tránh Msg 1785
    PRIMARY KEY (RobotID, ZoneID)
);
GO

-- 22. Maps: Bản đồ 2D của siêu thị phục vụ robot định vị
CREATE TABLE dbo.Maps (
    MapID      INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    FloorID    INT NOT NULL FOREIGN KEY REFERENCES dbo.Floors(FloorID), -- Bỏ CASCADE tránh Msg 1785
    MapName    NVARCHAR(100) NOT NULL,
    MapData    NVARCHAR(MAX) NULL, -- Lưu trữ dữ liệu JSON toạ độ các vật cản tĩnh
    CreatedAt  DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

-- 23. NavigationNodes: Các nút giao thông trên bản đồ dẫn đường (Dijkstra/A*)
-- Loại bỏ hoàn toàn cascading deletes để giải quyết triệt để lỗi Msg 1785 (Vòng lặp Cascade của SQL Server)
CREATE TABLE dbo.NavigationNodes (
    NodeID        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MapID         INT NOT NULL FOREIGN KEY REFERENCES dbo.Maps(MapID), -- Thay thế CASCADE bằng NO ACTION mặc định
    NodeName      NVARCHAR(100) NOT NULL, -- VD: 'Ngã tư A1-B1', 'Kệ sữa TH'
    XCoord        FLOAT NOT NULL,         -- Toạ độ X thực tế (mét)
    YCoord        FLOAT NOT NULL,         -- Toạ độ Y thực tế (mét)
    NodeType      NVARCHAR(20) NOT NULL,  -- 'intersection', 'shelf_front', 'station', 'entrance'
    LinkedAisleID INT NULL FOREIGN KEY REFERENCES dbo.Aisles(AisleID), -- Mặc định là ON DELETE NO ACTION (Không dùng SET NULL trực tiếp vì nhiều cascade path)
    IsBlocked     BIT NOT NULL DEFAULT 0  -- 1 = Đang kẹt đường / chặn robot
);
GO

-- 24. NavigationEdges: Các cạnh nối giữa 2 nút giao thông (độ dài thực tế làm trọng số)
CREATE TABLE dbo.NavigationEdges (
    EdgeID          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    FromNodeID      INT NOT NULL FOREIGN KEY REFERENCES dbo.NavigationNodes(NodeID) ON DELETE NO ACTION,
    ToNodeID        INT NOT NULL FOREIGN KEY REFERENCES dbo.NavigationNodes(NodeID) ON DELETE NO ACTION,
    Distance        FLOAT NOT NULL, -- Khoảng cách thực tế (mét)
    IsBidirectional BIT NOT NULL DEFAULT 1 -- 1 = Đi được cả 2 chiều, 0 = Đường 1 chiều
);
GO

-- 25. Workstations: Trạm sạc / Trạm dừng của robot trong siêu thị
CREATE TABLE dbo.Workstations (
    WorkstationID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ZoneID        INT NOT NULL FOREIGN KEY REFERENCES dbo.Zones(ZoneID), -- Thay thế CASCADE
    NodeID        INT NOT NULL FOREIGN KEY REFERENCES dbo.NavigationNodes(NodeID), -- Thay thế CASCADE
    StationName   NVARCHAR(100) NOT NULL
);
GO

-- 26. SemanticObjects: Các vật thể tĩnh được vẽ trên bản đồ siêu thị
CREATE TABLE dbo.SemanticObjects (
    ObjectID   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MapID      INT NOT NULL FOREIGN KEY REFERENCES dbo.Maps(MapID), -- Thay thế CASCADE
    ObjectType NVARCHAR(50) NOT NULL, -- 'shelf', 'wall', 'door', 'obstacle'
    XMin       FLOAT NOT NULL,
    YMin       FLOAT NOT NULL,
    XMax       FLOAT NOT NULL,
    YMax       FLOAT NOT NULL
);
GO

-- 2.7. Phân hệ ROBOT STATUS LOG (1 Table)

-- 27. Robot_Logs: Nhật ký trạng thái robot (Dữ liệu do ESP32 MQTT đẩy lên và n8n ghi vào)
-- Thiết kế vật lý trùng khớp 100% các cột cũ để n8n INSERT thẳng ko lỗi, đồng thời mở rộng trường cấu trúc
CREATE TABLE dbo.Robot_Logs (
    LogID         BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    RobotID       INT NULL FOREIGN KEY REFERENCES dbo.Robots(RobotID) ON DELETE SET NULL,
    battery       INT NULL,             -- Trùng tên cột trong n8n / MQTT payload
    location      NVARCHAR(255) NULL,   -- Trùng tên cột trong n8n / MQTT payload (tên node hiện tại)
    status        NVARCHAR(100) NULL,   -- Trùng tên cột trong n8n / MQTT payload ('online', 'offline', 'error')
    timestamp     DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Tương thích với AI DB
    CurrentNodeID INT NULL FOREIGN KEY REFERENCES dbo.NavigationNodes(NodeID) ON DELETE NO ACTION,
    Mode          NVARCHAR(20) NULL,    -- Trạng thái hành động cụ thể ('navigating', 'scanning'...)
    IsOnline      BIT NULL,
    XCoord        FLOAT NULL,           -- Toạ độ X thời gian thực (Dead Reckoning — Phase 2)
    YCoord        FLOAT NULL,           -- Toạ độ Y thời gian thực (Dead Reckoning — Phase 2)
    HeadingRad    FLOAT NULL            -- Heading robot (radian) từ Dead Reckoning — Phase 2
);
GO

-- Index tối ưu hóa truy vấn telemetry robot mới nhất phục vụ vẽ Dashboard live
CREATE INDEX IX_Robot_Logs_Robot_Timestamp ON dbo.Robot_Logs(RobotID, timestamp DESC);
GO

-- 2.8. Phân hệ SHELF SCAN (1 Table)

-- 28. ShelfScans: Lịch sử quét kệ hàng phát hiện hết hàng bằng Gemini AI Vision
CREATE TABLE dbo.ShelfScans (
    ScanID          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    AisleID         INT NOT NULL FOREIGN KEY REFERENCES dbo.Aisles(AisleID), -- Thay thế CASCADE tránh cycle
    ShelfLevelID    INT NULL FOREIGN KEY REFERENCES dbo.ShelfLevels(ShelfLevelID) ON DELETE NO ACTION,
    RobotID         INT NOT NULL FOREIGN KEY REFERENCES dbo.Robots(RobotID), -- Thay thế CASCADE tránh cycle
    ScannedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ImageUrl        NVARCHAR(500) NULL, -- Đường dẫn ảnh chụp kệ hàng lưu trên Azure Blob Storage
    EmptyPercentage DECIMAL(5,2) NOT NULL DEFAULT 0.00, -- Tỷ lệ trống phát hiện (0.00 - 100.00 %)
    NeedsRestock    AS CAST(CASE WHEN EmptyPercentage > 30.0 THEN 1 ELSE 0 END AS BIT), -- Computed column: Trống > 30% tự động báo restock
    AiResponseRaw   NVARCHAR(MAX) NULL, -- Phản hồi thô định dạng JSON của Gemini Vision để phân tích sâu
    -- Buổi 15: Trạng thái bị che khuất camera (người đứng chắn)
    IsOccluded      BIT NOT NULL DEFAULT 0,           -- 1 = bị che khuất, robot lên lịch quét lại
    OcclusionReason NVARCHAR(255) NULL                -- Nguyên nhân che khuất ghi nhận
);
GO

-- 2.9. Phân hệ SHOPPING & HISTORY (2 Tables)

-- 29. ShoppingHistories: Nhật ký giỏ hàng / hóa đơn mua sắm thực tế của hội viên
CREATE TABLE dbo.ShoppingHistories (
    ShoppingHistoryID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MemberID          INT NOT NULL FOREIGN KEY REFERENCES dbo.Members(MemberID), -- Bỏ CASCADE tránh cycle
    ShoppingDate      DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    TotalAmount       DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    PaymentMethod     NVARCHAR(50) NULL -- 'Cash', 'CreditCard', 'VNPAY', 'MOMO'
);
GO

-- 30. HistoryItems: Chi tiết sản phẩm trong từng lần mua sắm
CREATE TABLE dbo.HistoryItems (
    HistoryItemID     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ShoppingHistoryID INT NOT NULL FOREIGN KEY REFERENCES dbo.ShoppingHistories(ShoppingHistoryID) ON DELETE CASCADE,
    ProductID         INT NOT NULL FOREIGN KEY REFERENCES dbo.Products(ProductID), -- Bỏ CASCADE tránh cycle
    Quantity          INT NOT NULL DEFAULT 1,
    UnitPrice         DECIMAL(12,2) NOT NULL DEFAULT 0.00
);
GO

-- 2.10. Phân hệ RECIPE (2 Tables)

-- 31. Recipes: Thực đơn thông minh gợi ý món ăn dinh dưỡng cho hội viên
CREATE TABLE dbo.Recipes (
    RecipeID              INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    RecipeName            NVARCHAR(200) NOT NULL,
    Description           NVARCHAR(MAX) NULL,
    YieldPortions         INT NOT NULL DEFAULT 1, -- Khẩu phần ăn cho x người
    ImageUrl              NVARCHAR(500) NULL,
    -- Buổi 13: Thông số dinh dưỡng phục vụ Health-Centric Flow
    Calories              INT NULL,               -- Lượng calo ước tính toàn công thức
    HealthyScore          INT NULL,               -- Điểm lành mạnh 1-100
    AlternativeSuggestion NVARCHAR(500) NULL      -- Đề xuất thay thế lành mạnh hơn
);
GO

-- 32. RecipeItems: Nguyên liệu chi tiết cấu thành công thức nấu ăn
CREATE TABLE dbo.RecipeItems (
    RecipeID        INT NOT NULL FOREIGN KEY REFERENCES dbo.Recipes(RecipeID) ON DELETE CASCADE,
    ProductID       INT NOT NULL FOREIGN KEY REFERENCES dbo.Products(ProductID), -- Bỏ CASCADE tránh cycle
    QuantityRequired DECIMAL(10,2) NOT NULL, -- Số lượng cần thiết
    UnitOfMeasure   NVARCHAR(20) NOT NULL,   -- Đơn vị đo ('g', 'ml', 'quả', 'muỗng')
    PRIMARY KEY (RecipeID, ProductID)
);
GO

-- 2.11. Phân hệ PROMOTION & ADS (3 Tables)

-- 33. Promotions: Các chương trình khuyến mãi (VD: Khuyến mãi Black Friday)
CREATE TABLE dbo.Promotions (
    PromotionID   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    PromotionName NVARCHAR(200) NOT NULL,
    PromotionType NVARCHAR(50) NOT NULL, -- 'discount' (giảm %), 'flat_discount' (giảm tiền), 'buy1get1'
    DiscountValue DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    StartDate     DATE NOT NULL,
    EndDate       DATE NOT NULL,
    IsActive      BIT NOT NULL DEFAULT 1
);
GO

-- 34. PromotionProducts: Danh sách sản phẩm áp dụng chương trình khuyến mãi
CREATE TABLE dbo.PromotionProducts (
    PromotionID INT NOT NULL FOREIGN KEY REFERENCES dbo.Promotions(PromotionID) ON DELETE CASCADE,
    ProductID   INT NOT NULL FOREIGN KEY REFERENCES dbo.Products(ProductID), -- Bỏ CASCADE tránh cycle
    Priority    INT NOT NULL DEFAULT 0, -- Độ ưu tiên đề xuất sản phẩm lên app tablet robot
    PRIMARY KEY (PromotionID, ProductID)
);
GO

-- 35. SponsoredProducts: Sản phẩm được nhà sản xuất tài trợ quảng cáo (Mô hình kiếm tiền Ad Monetization)
CREATE TABLE dbo.SponsoredProducts (
    SponsoredID       INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ProductID         INT NOT NULL FOREIGN KEY REFERENCES dbo.Products(ProductID), -- Bỏ CASCADE tránh cycle
    SponsorBrand      NVARCHAR(100) NOT NULL,
    StartDate         DATE NOT NULL,
    EndDate           DATE NOT NULL,
    Priority          INT NOT NULL DEFAULT 0,
    IsActive          BIT NOT NULL DEFAULT 1,
    -- Buổi 14, 16, 17: Hệ thống đấu thầu quảng cáo đa chiều
    AdScore           INT NOT NULL DEFAULT 0,           -- Điểm quảng cáo cơ sở của nhãn hàng
    TimeSlotStart     TIME NULL,                        -- Khung giờ bắt đầu (NULL = cả ngày)
    TimeSlotEnd       TIME NULL,                        -- Khung giờ kết thúc
    IsWeekendOnly     BIT NOT NULL DEFAULT 0,           -- Chỉ hiển thị cuối tuần
    BidPrice          DECIMAL(12,2) NOT NULL DEFAULT 0.00, -- Giá đấu thầu mỗi lượt hiển thị
    WeekendMultiplier DECIMAL(3,2) NOT NULL DEFAULT 1.00   -- Hệ số nhân cuối tuần/ngày lễ
);

-- 36. ForbiddenZones: Vùng cấm robot đi vào theo toạ độ 2D (Buổi 10)
CREATE TABLE dbo.ForbiddenZones (
    ForbiddenZoneID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MapID           INT NOT NULL FOREIGN KEY REFERENCES dbo.Maps(MapID) ON DELETE CASCADE,
    ZoneName        NVARCHAR(100) NOT NULL,
    XMin            FLOAT NOT NULL,  -- Toạ độ góc trái dưới (mét)
    YMin            FLOAT NOT NULL,
    XMax            FLOAT NOT NULL,  -- Toạ độ góc phải trên (mét)
    YMax            FLOAT NOT NULL,
    IsActive        BIT NOT NULL DEFAULT 1,
    Reason          NVARCHAR(255) NULL -- Lý do cấm (VD: 'Khu vực trẻ em', 'Thi công tạm thời')
);
GO

-- 37. MemberAlerts: Lịch sử cảnh báo cá nhân hóa (Buổi 16)
CREATE TABLE dbo.MemberAlerts (
    AlertID      INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MemberID     INT NOT NULL FOREIGN KEY REFERENCES dbo.Members(MemberID) ON DELETE CASCADE,
    AlertType    NVARCHAR(50) NOT NULL, -- 'Allergy', 'BudgetExceeded', 'DuplicatePurchase', 'OutOfStock'
    AlertMessage NVARCHAR(500) NOT NULL,
    CreatedAt    DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsRead       BIT NOT NULL DEFAULT 0
);
GO

-- 38. MemberEvents: Sự kiện đặc biệt của hội viên để robot chủ động tặng ưu đãi (Buổi 16)
CREATE TABLE dbo.MemberEvents (
    EventID     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MemberID    INT NOT NULL FOREIGN KEY REFERENCES dbo.Members(MemberID) ON DELETE CASCADE,
    EventName   NVARCHAR(100) NOT NULL, -- 'Birthday', 'Anniversary', 'VIPUpgrade'
    EventDate   DATE NOT NULL,
    DiscountPct DECIMAL(5,2) NULL,      -- % giảm giá riêng cho sự kiện (VD: 15.00)
    IsProcessed BIT NOT NULL DEFAULT 0  -- 0 = chưa tặng, 1 = đã gửi coupon/chào mừng
);
GO

-- ============================================================================
-- PART 3: THIẾT KẾ SQL VIEWS TƯƠNG THÍCH NGƯỢC 100% CHO AI & n8n
-- Giải quyết triệt để vấn đề đồng bộ dữ liệu: n8n chỉ việc query các view này, 
-- Backend sẽ quản trị cấu trúc chuẩn hoá bên dưới mà không làm gãy hệ thống AI cũ.
-- ============================================================================

-- 3.1. PurchaseHistory View
-- Phục vụ n8n query TOP 5 sản phẩm mua gần đây của hội viên khi Face Login thành công
CREATE VIEW dbo.PurchaseHistory AS
SELECT 
    hi.HistoryItemID AS PurchaseID,
    sh.MemberID,
    p.ProductName,
    sh.ShoppingDate AS PurchaseDate
FROM dbo.HistoryItems hi
INNER JOIN dbo.ShoppingHistories sh ON hi.ShoppingHistoryID = sh.ShoppingHistoryID
INNER JOIN dbo.Products p ON hi.ProductID = p.ProductID;
GO

-- 3.2. Store_Map View
-- Phục vụ n8n query danh mục vị trí kệ hàng để nạp dữ liệu cho LangChain AI Navigation Agent
CREATE VIEW dbo.Store_Map AS
SELECT 
    p.ProductID AS MapID,
    p.ProductName,
    ISNULL(N'Kệ ' + a.AisleCode, N'Chưa xếp kệ') AS ShelfLocation,
    ISNULL(z.ZoneName, N'Khu vực chung') AS Landmark,
    ISNULL(a.AisleName, N'') AS AisleNote
FROM dbo.Products p
LEFT JOIN dbo.Slots sl ON p.ProductID = sl.ProductID
LEFT JOIN dbo.ShelfLevels slv ON sl.ShelfLevelID = slv.ShelfLevelID
LEFT JOIN dbo.Aisles a ON slv.AisleID = a.AisleID
LEFT JOIN dbo.Zones z ON a.ZoneID = z.ZoneID;
GO

-- 3.3. Blocked_Aisles View
-- Phục vụ n8n kiểm tra nhanh các dãy hàng bị chặn robot đi qua
CREATE VIEW dbo.Blocked_Aisles AS
SELECT 
    AisleID,
    AisleCode,
    IsBlocked,
    BlockReason AS Reason
FROM dbo.Aisles;
GO

-- 3.4. Real_Time_Stock View
-- Phục vụ n8n kiểm tra nhanh tồn kho và gợi ý sản phẩm thay thế của AI Agent
CREATE VIEW dbo.Real_Time_Stock AS
SELECT 
    p.ProductID AS StockID,
    p.ProductName,
    ISNULL((SELECT SUM(Quantity) FROM dbo.Slots WHERE ProductID = p.ProductID), 0) AS StockQuantity,
    sub.ProductName AS SubstituteProduct
FROM dbo.Products p
LEFT JOIN dbo.Products sub ON p.SubstituteProductID = sub.ProductID;
GO

-- ============================================================================
-- PART 4: HỆ THỐNG SEED DATA CHUẨN (SIÊU THỊ VIỆT NAM THỰC TẾ)
-- Khởi tạo đầy đủ sơ đồ phân khu, tầng kệ, danh mục, 25 sản phẩm mẫu, nhãn dinh dưỡng, 
-- và biểu đồ mạng lưới đường đi cho Robot di chuyển tự hành.
-- ============================================================================

-- 4.1. Khởi tạo Vai trò hệ thống
INSERT INTO dbo.Roles (RoleName, Description) VALUES 
(N'Admin', N'Quản trị viên tối cao'),
(N'Staff', N'Nhân viên điều phối/quét kệ siêu thị'),
(N'Member', N'Khách hàng hội viên thân thiết');
GO

-- 4.2. Khởi tạo Tài khoản & Hội viên (Mẫu ban chủ nhiệm đồ án)
-- Mật khẩu mã hoá giả lập: 'hash_pbkdf2_code_123'
INSERT INTO dbo.Users (Username, PasswordHash, Email, Phone, IsActive) VALUES 
('admin_lth', 'hash_pbkdf2_code_123', 'hieultse161727@fpt.edu.vn', '0986515253', 1),
('member_qhuy', 'hash_pbkdf2_code_123', 'huynqse160498@fpt.edu.vn', '0782766322', 1),
('member_ahung', 'hash_pbkdf2_code_123', 'hungnase180159@fpt.ecu.vn', '0868205403', 1),
('staff_dtnhan', 'hash_pbkdf2_code_123', 'nhandt35@fe.edu.vn', '0903056041', 1);
GO

-- Gắn vai trò cho tài khoản
INSERT INTO dbo.UserRoles (UserID, RoleID) VALUES 
(1, 1), -- admin_lth -> Admin
(2, 3), -- member_qhuy -> Member
(3, 3), -- member_ahung -> Member
(4, 2); -- staff_dtnhan -> Staff
GO

-- Tạo chi tiết hồ sơ Member (Hùng & Huy sẽ dùng khuôn mặt giả lập để AI quét khớp)
INSERT INTO dbo.Members (UserID, FullName, PhoneNumber, FacePath, FaceVector, Tier, TotalPoints) VALUES 
(2, N'Nguyễn Quang Huy', '0782766322', N'/storage/faces/huy_nq.jpg', N'[0.015, -0.042, 0.125, -0.098]', N'Gold', 1500),
(3, N'Nguyễn Anh Hùng', '0868205403', N'/storage/faces/hung_na.jpg', N'[-0.032, 0.088, 0.054, 0.112]', N'Silver', 800);
GO

-- Tạo hồ sơ Nhân viên (Thầy Đỗ Tấn Nhàn giám sát dự án đóng vai nhân viên kiểm kệ)
INSERT INTO dbo.Staff (UserID, FirstName, LastName, Phone, Email) VALUES 
(4, N'Nhàn', N'Đỗ Tấn', '0903056041', 'nhandt35@fe.edu.vn');
GO

-- 4.3. Thiết kế Cấu trúc Siêu thị & Phân khu (Store Layout)
INSERT INTO dbo.Supermarkets (SupermarketName, Address) VALUES 
(N'SmartMarket FPT Campus', N'Khu Công nghệ cao Hoà Lạc, Thạch Thất, Hà Nội');
GO

INSERT INTO dbo.Floors (SupermarketID, FloorNumber) VALUES (1, 1);
GO

INSERT INTO dbo.Zones (FloorID, ZoneCode, ZoneName, Description) VALUES 
(1, 'A', N'Thực phẩm tươi sống', N'Khu bán hoa quả, rau củ, thịt cá tươi sống'),
(1, 'B', N'Đồ uống & Giải khát', N'Khu nước ngọt, bia, sữa tươi và trà giải nhiệt'),
(1, 'C', N'Hóa mỹ phẩm & Đồ dùng', N'Khu dầu gội, nước rửa chén, chất tẩy rửa sinh hoạt'),
(1, 'D', N'Bánh kẹo & Đồ ăn vặt', N'Khu bánh ngọt, snack, kẹo dẻo cho trẻ em'),
(1, 'E', N'Gia vị & Đồ khô', N'Khu hạt nêm, nước mắm, mì tôm ăn liền');
GO

-- Tạo Dãy hàng (Aisles) cho mỗi phân khu
INSERT INTO dbo.Aisles (ZoneID, AisleCode, AisleName, IsBlocked, BlockReason) VALUES 
(1, 'A1', N'Dãy trái cây nhập khẩu', 0, NULL),
(1, 'A2', N'Dãy rau xanh hữu cơ', 0, NULL),
(2, 'B1', N'Kệ nước giải khát & Nước ngọt', 0, NULL),
(2, 'B2', N'Kệ sữa tươi & Sữa chua', 0, NULL),
(3, 'C1', N'Kệ hóa phẩm vệ sinh gia đình', 0, NULL),
(3, 'C2', N'Kệ dầu gội & Sữa tắm chăm sóc', 0, NULL),
(4, 'D1', N'Dãy bánh quy & Bánh xốp', 0, NULL),
(4, 'D2', N'Dãy khoai tây chiên & Snack', 0, NULL),
(5, 'E1', N'Kệ mì ăn liền & Đồ ăn khô', 0, NULL),
(5, 'E2', N'Kệ gia vị mắm muối bột ngọt', 0, NULL);
GO

-- Tạo tầng kệ (ShelfLevels) cho các dãy hàng (Mỗi dãy hàng có 3 tầng kệ trưng bày)
INSERT INTO dbo.ShelfLevels (AisleID, LevelNumber) VALUES 
(1, 1), (1, 2), (1, 3), -- Dãy A1
(2, 1), (2, 2), (2, 3), -- Dãy A2
(3, 1), (3, 2), (3, 3), -- Dãy B1
(4, 1), (4, 2), (4, 3), -- Dãy B2
(5, 1), (5, 2), (5, 3), -- Dãy C1
(6, 1), (6, 2), (6, 3), -- Dãy C2
(7, 1), (7, 2), (7, 3), -- Dãy D1
(8, 1), (8, 2), (8, 3), -- Dãy D2
(9, 1), (9, 2), (9, 3), -- Dãy E1
(10, 1), (10, 2), (10, 3); -- Dãy E2
GO

-- 4.4. Định nghĩa Nhãn Dinh dưỡng (Health Tags)
INSERT INTO dbo.HealthTags (TagName, TagType) VALUES 
(N'Không đường', 'diet'),
(N'Ít béo', 'diet'),
(N'Thuần chay (Vegan)', 'diet'),
(N'Organic Hữu cơ', 'diet'),
(N'Dị ứng sữa', 'allergy'),
(N'Dị ứng hạt lạc', 'allergy'),
(N'Dị ứng hải sản', 'allergy');
GO

-- Gắn sở thích ăn kiêng cho Huy (Thích đồ ăn không đường và hữu cơ)
INSERT INTO dbo.MemberHealthPreferences (MemberID, TagID, IsAllergy) VALUES 
(1, 1, 0), -- Huy thích Không đường
(1, 4, 0); -- Huy thích Organic
-- Cấu hình dị ứng cho Hùng (Hùng bị dị ứng hạt lạc)
INSERT INTO dbo.MemberHealthPreferences (MemberID, TagID, IsAllergy) VALUES 
(2, 6, 1); -- Hùng bị dị ứng lạc (Hệ thống sẽ quét check và cảnh báo nếu Hùng mua hạt)
GO

-- 4.5. Xây dựng Danh mục Sản phẩm 4 Cấp (25+ Sản phẩm Việt Nam thực tế)

-- Cấp 1: Categories
INSERT INTO dbo.Categories (CategoryName, Description) VALUES 
(N'Hàng Tiêu Dùng Nhanh (FMCG)', N'Sản phẩm thiết yếu ăn uống hàng ngày'),
(N'Hóa Mỹ Phẩm & Chăm Sóc', N'Vật dụng tẩy rửa gia đình và vệ sinh cá nhân');
GO

-- Cấp 2: Subcategories
INSERT INTO dbo.Subcategories (CategoryID, SubcategoryName) VALUES 
(1, N'Nước Giải Khát & Đồ Uống'),
(1, N'Sữa & Sản phẩm từ Sữa'),
(1, N'Mì Ăn Liền & Đồ Khô'),
(1, N'Bánh Kẹo & Đồ Ăn Vặt'),
(2, N'Hóa Phẩm Gia Đình');
GO

-- Cấp 3: ProductTypes
INSERT INTO dbo.ProductTypes (SubcategoryID, ProductTypeName) VALUES 
(1, N'Nước ngọt có ga'),
(1, N'Trà & Nước trái cây đóng chai'),
(2, N'Sữa tươi tiệt trùng'),
(2, N'Sữa chua ăn'),
(3, N'Mì tôm gói'),
(3, N'Đồ khô đóng gói'),
(4, N'Bánh ngọt công nghiệp'),
(4, N'Snack khoai tây chiên'),
(5, N'Nước rửa chén vệ sinh');
GO

-- Cấp 4: Products (25+ sản phẩm đầy đủ thông tin barcode, khối lượng, ảnh chụp)
INSERT INTO dbo.Products (ProductTypeID, ProductName, UnitPrice, Barcode, ImageUrl, WeightOrVolume, Unit, Description) VALUES 
(1, N'Nước ngọt Coca Cola lon', 10000.00, '8935049500015', N'/images/products/coca_cola.jpg', 320.00, 'ml', N'Nước ngọt giải khát có ga truyền thống hương vị thơm ngon'),
(1, N'Nước ngọt Pepsi lon', 9500.00, '8935049500022', N'/images/products/pepsi.jpg', 320.00, 'ml', N'Nước giải khát có ga mát lạnh sảng khoái cực đỉnh'),
(1, N'Nước ngọt Coca Cola Không Đường', 10500.00, '8935049500039', N'/images/products/coca_zero.jpg', 320.00, 'ml', N'Phiên bản nước ngọt ga không đường tốt cho sức khỏe ăn kiêng'),
(2, N'Trà xanh không độ chai', 12000.00, '8935049500046', N'/images/products/tra_xanh_0d.jpg', 455.00, 'ml', N'Trà xanh chiết xuất từ lá trà tươi mát lành, chứa chất chống oxy hóa'),
(3, N'Sữa tươi tiệt trùng TH True Milk ít đường', 38000.00, '8935049500053', N'/images/products/th_it_duong.jpg', 1000.00, 'ml', N'Sữa tươi tiệt trùng làm từ sữa sạch nguyên chất của trang trại TH'),
(3, N'Sữa tươi tiệt trùng Vinamilk Không Đường', 36000.00, '8935049500060', N'/images/products/vnm_khong_duong.jpg', 1000.00, 'ml', N'Sữa tươi nguyên chất tiệt trùng 100% không thêm đường thơm ngon tự nhiên'),
(4, N'Sữa chua ăn Vinamilk có đường hộp', 8000.00, '8935049500077', N'/images/products/vnm_yogurt.jpg', 100.00, 'g', N'Sữa chua thơm ngon bổ sung men vi sinh hỗ trợ tiêu hóa tốt'),
(5, N'Mì tôm Hảo Hảo tôm chua cay gói', 4500.00, '8935049500084', N'/images/products/hao_hao.jpg', 75.00, 'g', N'Mì ăn liền quốc dân hương vị tôm chua cay đậm đà khó cưỡng'),
(5, N'Mì trộn Omachi xốt sườn hầm gói', 8500.00, '8935049500091', N'/images/products/omachi_suon.jpg', 80.00, 'g', N'Mì làm từ khoai tây hảo hạng dai ngon kết hợp nước xốt sườn hầm thơm phức'),
(6, N'Gạo ST25 Ông Cua túi cao cấp', 185000.00, '8935049500107', N'/images/products/gao_st25.jpg', 5.00, 'kg', N'Gạo đạt giải gạo ngon nhất thế giới, hạt cơm dẻo thơm hương dứa'),
(7, N'Bánh ChocoPie Orion hộp lớn', 55000.00, '8935049500114', N'/images/products/chocopie.jpg', 396.00, 'g', N'Bánh socola ngọt ngào hòa quyện cùng lớp kem dẻo marshmallow hấp dẫn'),
(7, N'Bánh trứng Custas Orion hộp', 48000.00, '8935049500121', N'/images/products/custas.jpg', 141.00, 'g', N'Bánh bông lan mềm mại nhân kem trứng ngọt ngào, thơm lừng béo ngậy'),
(8, N'Snack khoai tây Lays tự nhiên gói', 15000.00, '8935049500138', N'/images/products/lays_classic.jpg', 63.00, 'g', N'Lát khoai tây vàng giòn tẩm ướp muối tinh khiết giòn rụm thơm ngon'),
(8, N'Đậu phộng Tân Tân vị nước cốt dừa', 18000.00, '8935049500145', N'/images/products/tan_tan_dua.jpg', 100.00, 'g', N'Hạt đậu phộng giòn bùi kết hợp hương thơm béo ngậy của nước cốt dừa'),
(9, N'Nước rửa chén Sunlight chanh chai', 32000.00, '8935049500152', N'/images/products/sunlight_chanh.jpg', 750.00, 'ml', N'Sức mạnh tẩy sạch dầu mỡ siêu tốc từ tinh chất chanh tươi mát rượi');
GO

-- Bổ sung thêm các sản phẩm thay thế (Cấu hình thay thế khi hết hàng cho AI Agent gợi ý)
UPDATE dbo.Products SET SubstituteProductID = 2 WHERE ProductID = 1;
UPDATE dbo.Products SET SubstituteProductID = 1 WHERE ProductID = 2;
UPDATE dbo.Products SET SubstituteProductID = 6 WHERE ProductID = 5;
UPDATE dbo.Products SET SubstituteProductID = 9 WHERE ProductID = 8;
GO

-- Thiết lập Nhãn dinh dưỡng tương thích cho Sản phẩm
INSERT INTO dbo.ProductHealthTags (ProductID, TagID) VALUES 
(3, 1), -- Coca Không đường có tag Không đường
(3, 2), -- Coca Không đường có tag Ít béo
(6, 1), -- Sữa Vinamilk Không đường có tag Không đường
(6, 2), -- Sữa Vinamilk Không đường có tag Ít béo
(6, 4), -- Sữa Vinamilk Không đường có tag Organic
(14, 6); -- Đậu phộng Tân Tân chứa dị ứng hạt lạc (Hùng sẽ bị cảnh báo!)
GO

-- 4.6. Xếp sản phẩm lên các ô trên kệ (Slots) và điền số lượng tồn kho
-- Dãy B1 trưng bày Nước ngọt:
INSERT INTO dbo.Slots (ShelfLevelID, SlotCode, ProductID, Quantity, LastScannedAt) VALUES 
(7, '01', 1, 15, GETUTCDATE()),  -- Tầng 1 ô 1: Coca Cola
(7, '02', 2, 8, GETUTCDATE()),   -- Tầng 1 ô 2: Pepsi
(8, '01', 3, 22, GETUTCDATE()),  -- Tầng 2 ô 1: Coca Không Đường
(9, '01', 4, 14, GETUTCDATE());  -- Tầng 3 ô 1: Trà xanh không độ

-- Dãy B2 trưng bày Sữa:
INSERT INTO dbo.Slots (ShelfLevelID, SlotCode, ProductID, Quantity, LastScannedAt) VALUES 
(10, '01', 5, 10, GETUTCDATE()), -- Tầng 1 ô 1: Sữa TH ít đường
(11, '01', 6, 2, GETUTCDATE()),  -- Tầng 2 ô 1: Sữa Vinamilk không đường (Số lượng ít báo động!)
(12, '01', 7, 30, GETUTCDATE()); -- Tầng 3 ô 1: Sữa chua Vinamilk

-- Dãy E1 trưng bày Mì ăn liền và đồ khô:
INSERT INTO dbo.Slots (ShelfLevelID, SlotCode, ProductID, Quantity, LastScannedAt) VALUES 
(25, '01', 8, 45, GETUTCDATE()), -- Tầng 1 ô 1: Mì Hảo Hảo
(25, '02', 9, 20, GETUTCDATE()), -- Tầng 1 ô 2: Mì Omachi
(26, '01', 10, 5, GETUTCDATE()); -- Tầng 2 ô 1: Gạo ST25

-- Dãy D1 trưng bày Bánh kẹo:
INSERT INTO dbo.Slots (ShelfLevelID, SlotCode, ProductID, Quantity, LastScannedAt) VALUES 
(19, '01', 11, 16, GETUTCDATE()), -- Tầng 1 ô 1: Bánh Chocopie
(20, '01', 12, 12, GETUTCDATE()), -- Tầng 2 ô 1: Bánh trứng Custas
(21, '01', 13, 0, GETUTCDATE());  -- Tầng 3 ô 1: Snack Lays (Cháy hàng - Hết hàng!)

-- Dãy D2 trưng bày Đậu phộng hạt:
INSERT INTO dbo.Slots (ShelfLevelID, SlotCode, ProductID, Quantity, LastScannedAt) VALUES 
(22, '01', 14, 25, GETUTCDATE()); -- Tầng 1 ô 1: Đậu phộng Tân Tân

-- Dãy C1 trưng bày Hoá phẩm:
INSERT INTO dbo.Slots (ShelfLevelID, SlotCode, ProductID, Quantity, LastScannedAt) VALUES 
(13, '01', 15, 19, GETUTCDATE()); -- Tầng 1 ô 1: Rửa chén Sunlight
GO

-- 4.7. Khởi tạo Robot tự hành (Robot thiết kế theo mạch ESP32-S3)
INSERT INTO dbo.Robots (RobotName, RobotCode, MacAddress, BatteryPct, Mode, IsOnline, LastSeenAt) VALUES 
(N'SmartBot 4WD V1', 'ROBOT-01', '30:AE:A4:07:0F:70', 88, 'idle', 1, GETUTCDATE());
GO

-- Phân vùng cho Robot ROBOT-01 hoạt động trên Phân khu B và D
INSERT INTO dbo.RobotZones (RobotID, ZoneID) VALUES 
(1, 2), -- Zone B
(1, 4); -- Zone D
GO

-- 4.8. Bản đồ và Biểu đồ dẫn đường thông minh (Navigation Graph cho Robot)
INSERT INTO dbo.Maps (FloorID, MapName, MapData) VALUES 
(1, N'Bản đồ Tầng 1 chính thức', N'{"grid_width": 20, "grid_height": 20, "obstacle_count": 8}');
GO

-- Khởi tạo danh sách các Nút Giao Thông (Nodes) trên luồng đi của Robot
-- Toạ độ đo bằng mét (x, y) bắt đầu từ điểm xuất phát của siêu thị
INSERT INTO dbo.NavigationNodes (MapID, NodeName, XCoord, YCoord, NodeType, LinkedAisleID, IsBlocked) VALUES 
(1, N'Cửa ra vào siêu thị (Entrance)', 0.0, 0.0, 'entrance', NULL, 0),          -- NodeID 1
(1, N'Trạm sạc tự động (Home Dock)', 1.0, 0.0, 'station', NULL, 0),            -- NodeID 2
(1, N'Ngã tư trung tâm Phân khu A', 3.0, 3.0, 'intersection', NULL, 0),        -- NodeID 3
(1, N'Trước kệ trái cây dãy A1', 5.0, 3.0, 'shelf_front', 1, 0),               -- NodeID 4
(1, N'Trước kệ rau củ dãy A2', 7.0, 3.0, 'shelf_front', 2, 0),                 -- NodeID 5
(1, N'Ngã tư lối đi dãy B1', 3.0, 7.0, 'intersection', NULL, 0),               -- NodeID 6
(1, N'Trước kệ nước giải khát dãy B1', 5.0, 7.0, 'shelf_front', 3, 0),         -- NodeID 7
(1, N'Trước kệ sữa tươi dãy B2', 7.0, 7.0, 'shelf_front', 4, 0),               -- NodeID 8
(1, N'Ngã ba hành lang hoá chất C1', 3.0, 11.0, 'intersection', NULL, 0),      -- NodeID 9
(1, N'Trước kệ rửa chén Sunlight dãy C1', 5.0, 11.0, 'shelf_front', 5, 0);     -- NodeID 10
GO

-- Cấu hình Cạnh nối (Edges) biểu diễn đường đi thực tế và khoảng cách vật lý của chúng
-- Phục vụ thuật toán Dijkstra của Robot tìm đường đi ngắn nhất
INSERT INTO dbo.NavigationEdges (FromNodeID, ToNodeID, Distance, IsBidirectional) VALUES 
(1, 2, 1.0, 1),  -- Cửa vào <-> Trạm sạc (1 mét)
(1, 3, 4.2, 1),  -- Cửa vào <-> Ngã tư Phân khu A (4.2 mét)
(3, 4, 2.0, 1),  -- Ngã tư A <-> Kệ trái cây A1 (2 mét)
(4, 5, 2.0, 1),  -- Kệ trái cây A1 <-> Kệ rau củ A2 (2 mét)
(3, 6, 4.0, 1),  -- Ngã tư A <-> Ngã tư lối đi dãy B1 (4 mét)
(6, 7, 2.0, 1),  -- Ngã tư B <-> Kệ nước giải khát B1 (2 mét)
(7, 8, 2.0, 1),  -- Kệ nước B1 <-> Kệ sữa B2 (2 mét)
(6, 9, 4.0, 1),  -- Ngã tư B <-> Ngã ba hành lang C1 (4 mét)
(9, 10, 2.0, 1); -- Ngã ba C <-> Kệ Sunlight C1 (2 mét)
GO

-- Cài đặt Trạm sạc robot hoạt động vật lý
INSERT INTO dbo.Workstations (ZoneID, NodeID, StationName) VALUES 
(2, 2, N'Trạm sạc số 1 SmartMarket');
GO

-- 4.9. Tạo dữ liệu Lịch sử Mua sắm (Shopping History) của Hội viên
-- Để Gemini AI có dữ liệu cũ và tiến hành gợi ý sản phẩm khi khách login
-- Huy mua sắm ngày hôm qua: Mua Coca Không Đường và Bánh Chocopie
INSERT INTO dbo.ShoppingHistories (MemberID, ShoppingDate, TotalAmount, PaymentMethod) VALUES 
(1, DATEADD(day, -1, GETUTCDATE()), 76000.00, 'MOMO'); -- ShoppingHistoryID = 1
GO

INSERT INTO dbo.HistoryItems (ShoppingHistoryID, ProductID, Quantity, UnitPrice) VALUES 
(1, 3, 2, 10500.00), -- 2 lon Coca không đường (phục vụ Huy ăn kiêng)
(1, 11, 1, 55000.00); -- 1 hộp bánh ChocoPie
GO

-- Hùng mua sắm 5 ngày trước: Mua mì tôm Hảo Hảo
INSERT INTO dbo.ShoppingHistories (MemberID, ShoppingDate, TotalAmount, PaymentMethod) VALUES 
(2, DATEADD(day, -5, GETUTCDATE()), 90000.00, 'VNPAY'); -- ShoppingHistoryID = 2
GO

INSERT INTO dbo.HistoryItems (ShoppingHistoryID, ProductID, Quantity, UnitPrice) VALUES 
(2, 8, 20, 4500.00); -- 20 gói mì tôm Hảo Hảo (trữ trữ mì)
GO

-- 4.10. Định nghĩa Thực đơn Dinh dưỡng mẫu (Recipes & RecipeItems)
-- Phục vụ chức năng "Thực đơn dinh dưỡng" gợi ý mua nguyên liệu trên app tablet robot
INSERT INTO dbo.Recipes (RecipeName, Description, YieldPortions, ImageUrl, Calories, HealthyScore, AlternativeSuggestion) VALUES 
(N'Món mì trộn chua cay đặc biệt', N'Công thức chế biến đĩa mì xốt trộn giòn bùi phối trộn xúc xích rau xanh cực nhanh', 2, N'/images/recipes/mi_tron.jpg', 520, 45, N'Thay mì tôm bằng mì khoai tây Omachi để giảm dầu mỡ');
GO

-- Đăng ký nguyên liệu cho công thức mì trộn đặc biệt
INSERT INTO dbo.RecipeItems (RecipeID, ProductID, QuantityRequired, UnitOfMeasure) VALUES 
(1, 8, 2.00, N'gói'), -- Cần 2 gói mì Hảo Hảo
(1, 14, 0.5, N'gói'); -- Rải thêm 1/2 gói đậu phộng Tân Tân giòn giòn béo béo
GO

-- 4.11. Khởi tạo Chương trình Khuyến mãi & Quảng cáo (Promotions & Sponsored Products)
-- Phục vụ tablet robot hiển thị pop-up ưu đãi khi robot dẫn khách đi ngang qua dãy
INSERT INTO dbo.Promotions (PromotionName, PromotionType, DiscountValue, StartDate, EndDate, IsActive) VALUES 
(N'Ưu đãi giải nhiệt mùa hè', 'discount', 10.00, CAST(GETUTCDATE() AS DATE), CAST(DATEADD(month, 2, GETUTCDATE()) AS DATE), 1); -- Giảm 10%
GO

-- Áp dụng cho Trà xanh không độ và Coca cola
INSERT INTO dbo.PromotionProducts (PromotionID, ProductID, Priority) VALUES 
(1, 4, 1), -- Trà xanh không độ được ưu tiên cao
(1, 1, 2); -- Coca cola
GO

-- Đăng ký sản phẩm quảng cáo tài trợ (Sponsored Products)
-- Hãng Orion tài trợ đẩy mạnh đề xuất Custas — khung giờ sáng + cuối tuần x1.5
INSERT INTO dbo.SponsoredProducts (ProductID, SponsorBrand, StartDate, EndDate, Priority, IsActive, AdScore, TimeSlotStart, TimeSlotEnd, IsWeekendOnly, BidPrice, WeekendMultiplier) VALUES 
(12, N'Orion Vina', CAST(GETUTCDATE() AS DATE), CAST(DATEADD(month, 1, GETUTCDATE()) AS DATE), 5, 1, 85, '07:00:00', '12:00:00', 0, 500.00, 1.50);
GO

-- 4.12. Seed dữ liệu 3 bảng mới

-- ForbiddenZones: 2 vùng cấm robot mẫu trên Bản đồ Tầng 1
INSERT INTO dbo.ForbiddenZones (MapID, ZoneName, XMin, YMin, XMax, YMax, IsActive, Reason) VALUES 
(1, N'Khu vực quầy thu ngân', 8.0, 0.0, 12.0, 3.0, 1, N'Robot không được đi gần quầy thu ngân khi có khách'),
(1, N'Hành lang thoát hiểm', 0.0, 8.0, 2.0, 20.0, 1, N'Lối thoát hiểm bắt buộc luôn thông thoáng');
GO

-- MemberAlerts: Cảnh báo dị ứng cho Hùng (MemberID=2) khi quét đậu phộng
INSERT INTO dbo.MemberAlerts (MemberID, AlertType, AlertMessage, IsRead) VALUES 
(2, 'Allergy', N'⚠️ CẢNH BÁO DỊ ỨNG: Đậu phộng Tân Tân (Mã: 8935049500145) chứa hạt lạc — Hùng đã được ghi nhận dị ứng hạt lạc!', 0),
(1, 'BudgetExceeded', N'💰 Tổng giỏ hàng hiện tại 210.000₫ đã vượt ngân sách 200.000₫ bạn đã cài đặt.', 0);
GO

-- MemberEvents: Sinh nhật Huy và kỷ niệm Hùng
INSERT INTO dbo.MemberEvents (MemberID, EventName, EventDate, DiscountPct, IsProcessed) VALUES 
(1, 'Birthday', CAST(DATEADD(day, 3, GETUTCDATE()) AS DATE), 15.00, 0),  -- Huy sinh nhật sau 3 ngày → robot chào mừng
(2, 'Anniversary', CAST(DATEADD(day, 7, GETUTCDATE()) AS DATE), 10.00, 0); -- Hùng kỷ niệm 1 năm hội viên
GO

-- Cập nhật Members: SearchMode & ShoppingBudget cho Huy và Hùng
UPDATE dbo.Members SET SearchMode = 'Healthy', ShoppingBudget = 200000.00 WHERE MemberID = 1; -- Huy chế độ lành mạnh, ngân sách 200k
UPDATE dbo.Members SET SearchMode = 'Budget',  ShoppingBudget = 150000.00 WHERE MemberID = 2; -- Hùng chế độ tiết kiệm, ngân sách 150k
GO

-- Cập nhật Slots: thêm ExpiryDate + Supplier cho các ô quan trọng
UPDATE dbo.Slots SET ExpiryDate = CAST(DATEADD(day, 30, GETUTCDATE()) AS DATE), Supplier = N'Công ty CP Sữa Việt Nam (Vinamilk)' WHERE ProductID IN (5, 6, 7);
UPDATE dbo.Slots SET ExpiryDate = CAST(DATEADD(day, 180, GETUTCDATE()) AS DATE), Supplier = N'Acecook Việt Nam' WHERE ProductID IN (8, 9);
UPDATE dbo.Slots SET ExpiryDate = CAST(DATEADD(day, 365, GETUTCDATE()) AS DATE), Supplier = N'Tân Tân Food' WHERE ProductID = 14;
GO

-- ============================================================================
-- PHẦN CUỐI: HOÀN TẤT THIẾT LẬP DATABASE
-- ============================================================================
PRINT '====================================================================';
PRINT '  SUCCESS: DATABASE [SuperMarketBot] MERGED SCHEMA INITIALIZED!';
PRINT '  38 Tables, 4 Dynamic Views & Full Supermarket Seed Data Ready!';
PRINT '  V4.0 - Capstone Full Schema (Buổi 10, 13, 14, 15, 16, 17)';
PRINT '====================================================================';
GO
