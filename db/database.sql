-- ============================================================================
-- SMARTMARKETBOT - ONE-SHOT DATABASE SCRIPT (SQL SERVER 2019+)
-- ============================================================================
-- Mục đích  : Tạo toàn bộ schema + seed data cho cả nhóm, CHẠY 1 PHÁT LÀ OK.
-- Phiên bản : V4.0 (2026-06-16) - One-shot rebuild, idempotent, fresh install
-- Tương thích:
--    * Backend .NET 10 (SmartMarketBot.API) - DbContext: AppDbContext.cs
--    * AI FastAPI + n8n (đọc qua 4 View: PurchaseHistory, Store_Map,
--                                            Blocked_Aisles, Real_Time_Stock)
--    * Mobile (React Native) + IoT (ESP32-S3 MQTT telemetry → ROBOT_LOG)
--
-- CÁCH CHẠY (chọn 1 trong 4):
--   1. SQL Server Management Studio (SSMS)
--        File → Open → chọn file này → F5 (Execute)
--   2. Azure Data Studio
--        File → Open → chọn file này → Ctrl+Shift+E
--   3. Command line (sqlcmd)
--        sqlcmd -S localhost -E -i "database.sql"
--   4. PowerShell
--        sqlcmd -S localhost -E -i "$PWD\database.sql"
--
-- LƯU Ý:
--   - Chạy với quyền sysadmin (để CREATE DATABASE).
--   - Có thể chạy lại nhiều lần: script sẽ xóa DB cũ rồi tạo lại sạch sẽ.
--   - Tên DB mặc định: SuperMarketBot (đổi biến @DBName ở dưới nếu cần).
--   - Cần SQL Server 2019 trở lên (vì dùng DATETIME2, GETUTCDATE, computed PERSISTED).
--   - Style đặt tên: SNAKE_CASE (UPPER_CASE cho bảng, snake_case cho cột)
--     theo đúng ERD mới nhất (V4.0).
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ============================================================================
-- PHẦN 0: CẤU HÌNH + KIỂM TRA MÔI TRƯỜNG
-- ============================================================================
DECLARE @DBName           NVARCHAR(128) = N'SuperMarketBot';
DECLARE @MinMajorVersion  INT = 15;            -- SQL Server 2019 = 15
DECLARE @ServerMajorVer   INT = CAST(SERVERPROPERTY('ProductMajorVersion') AS INT);
DECLARE @ServerEdition    NVARCHAR(200) = CAST(SERVERPROPERTY('Edition') AS NVARCHAR(200));

IF @ServerMajorVer < @MinMajorVersion
BEGIN
    RAISERROR(N'❌ Yêu cầu SQL Server 2019 trở lên. Hiện tại đang dùng major version = %d (%s).', 16, 1, @ServerMajorVer, @ServerEdition);
    SET NOEXEC ON;
END

PRINT N'✅ SQL Server version: ' + CAST(@ServerMajorVer AS NVARCHAR(10)) + N' - ' + @ServerEdition;
PRINT N'✅ Database sẽ tạo: ' + @DBName;
PRINT N'✅ Style đặt tên: SNAKE_CASE (theo ERD V4.0)';
GO

-- ============================================================================
-- PHẦN 1: TẠO DATABASE (nếu chưa có) + CHUYỂN SANG DB
-- ============================================================================
DECLARE @DBName NVARCHAR(128) = N'SuperMarketBot';

IF DB_ID(@DBName) IS NULL
BEGIN
    DECLARE @sqlCreate NVARCHAR(MAX) = N'CREATE DATABASE [' + @DBName + N']
        COLLATE SQL_Latin1_General_CP1_CI_AS
        WITH RECOVERY FULL;';
    EXEC sp_executesql @sqlCreate;
    PRINT N'✅ Đã tạo database: ' + @DBName;
END
ELSE
BEGIN
    PRINT N'ℹ️  Database đã tồn tại, sẽ xóa sạch và tạo lại: ' + @DBName;
END
GO

USE [SuperMarketBot];
GO

ALTER DATABASE CURRENT SET QUOTED_IDENTIFIER ON;
ALTER DATABASE CURRENT SET ANSI_NULLS ON;
ALTER DATABASE CURRENT SET RECOVERY SIMPLE;
GO

-- ============================================================================
-- PHẦN 2: XÓA SẠCH (theo thứ tự ngược FK) - Idempotent
-- ============================================================================
PRINT N'🗑️  PHẦN 2: Dọn dẹp view + table cũ (nếu có)...';

-- Xóa View trước
DROP VIEW IF EXISTS dbo.Real_Time_Stock;
DROP VIEW IF EXISTS dbo.Blocked_Aisles;
DROP VIEW IF EXISTS dbo.Store_Map;
DROP VIEW IF EXISTS dbo.PurchaseHistory;
PRINT N'   - Đã xóa 4 view';
GO

-- Xóa Table theo thứ tự ngược FK dependency (con trước, cha sau)
-- Tổng cộng 37 bảng theo ERD V4.0
DROP TABLE IF EXISTS dbo.AD_CAMPAIGN_LOG;
DROP TABLE IF EXISTS dbo.SPONSORED_PRODUCT;
DROP TABLE IF EXISTS dbo.AD_CAMPAIGN;
DROP TABLE IF EXISTS dbo.ROUTE_ASSIGNMENT;
DROP TABLE IF EXISTS dbo.ROUTE_NODE_MAPPING;
DROP TABLE IF EXISTS dbo.ROBOT_ROUTE;
DROP TABLE IF EXISTS dbo.AISLE_NODE;
DROP TABLE IF EXISTS dbo.NAVIGATION_EDGE;
DROP TABLE IF EXISTS dbo.NAVIGATION_NODE;
DROP TABLE IF EXISTS dbo.SEMANTIC_OBJECT;
DROP TABLE IF EXISTS dbo.MAP;
DROP TABLE IF EXISTS dbo.AISLE_SCAN;
DROP TABLE IF EXISTS dbo.ROBOT_LOG;
DROP TABLE IF EXISTS dbo.ROBOT_ZONE;
DROP TABLE IF EXISTS dbo.ROBOT;
DROP TABLE IF EXISTS dbo.MEAL_ITEM;
DROP TABLE IF EXISTS dbo.MEAL_SUGGESTION;
DROP TABLE IF EXISTS dbo.INVOICE_HISTORY_ITEM;
DROP TABLE IF EXISTS dbo.INVOICE_HISTORY;
DROP TABLE IF EXISTS dbo.PRODUCT_SLOT;
DROP TABLE IF EXISTS dbo.SLOT;
DROP TABLE IF EXISTS dbo.SHELF;
DROP TABLE IF EXISTS dbo.AISLE;
DROP TABLE IF EXISTS dbo.ZONE;
DROP TABLE IF EXISTS dbo.FLOOR;
DROP TABLE IF EXISTS dbo.PRODUCT_HEALTHTAG;
DROP TABLE IF EXISTS dbo.MEMBERHEALTH_PREFERENCE;
DROP TABLE IF EXISTS dbo.HEALTH_TAG;
DROP TABLE IF EXISTS dbo.PRODUCT;
DROP TABLE IF EXISTS dbo.PRODUCT_TYPE;
DROP TABLE IF EXISTS dbo.SUBCATEGORY;
DROP TABLE IF EXISTS dbo.CATEGORY;
DROP TABLE IF EXISTS dbo.MEMBERSHIP;
DROP TABLE IF EXISTS dbo.MEMBER;
DROP TABLE IF EXISTS dbo.ACCOUNT;
DROP TABLE IF EXISTS dbo.AD_PACKAGE;
DROP TABLE IF EXISTS dbo.BRAND;
PRINT N'   - Đã xóa 37 table (theo ERD V4.0)';
GO

-- ============================================================================
-- PHẦN 3: TẠO BẢNG THEO THỨ TỰ FK DEPENDENCY (cha → con)
-- ============================================================================
-- Quy ước (theo ERD V4.0):
--   * Tên bảng: UPPER_CASE SNAKE_CASE (vd: INVOICE_HISTORY, PRODUCT_HEALTHTAG)
--   * Tên cột: snake_case (vd: account_id, member_id, unit_price)
--   * PK: {tên_bảng_ít}_id (vd: AccountID, MemberID) - giữ PascalCase theo ERD
--   * FK: CONSTRAINT FK_{BảngCon}_{BảngCha} tường minh
--   * Cascade: NO ACTION mặc định (chống Msg 1785), chỉ dùng CASCADE khi
--              không tạo cycle
--   * Bool: BIT NOT NULL DEFAULT 0/1
--   * DateTime: DATETIME2 NOT NULL DEFAULT GETUTCDATE()
--   * Index: unique + composite + filtered
-- ============================================================================
PRINT N'🏗️  PHẦN 3: Tạo bảng (37 bảng theo ERD V4.0)...';
GO

-- ─────────────────────────────────────────────────────
-- REGION 1: IDENTITY & MEMBERSHIP (4 Tables)
-- ─────────────────────────────────────────────────────

-- 1. ACCOUNT: Tài khoản hệ thống (Role: 'Admin'/'Staff'/'Member' theo ERD)
CREATE TABLE dbo.ACCOUNT (
    account_id   INT IDENTITY(1,1) NOT NULL,
    username     NVARCHAR(100) NOT NULL,
    password_hash NVARCHAR(500) NOT NULL,
    email        NVARCHAR(256) NULL,
    phone        NVARCHAR(20)  NULL,
    full_name    NVARCHAR(100) NULL,
    is_active    BIT           NOT NULL CONSTRAINT DF_ACCOUNT_is_active DEFAULT 1,
    role         NVARCHAR(20)  NOT NULL CONSTRAINT DF_ACCOUNT_role      DEFAULT 'Member',
    created_at   DATETIME2     NOT NULL CONSTRAINT DF_ACCOUNT_created_at DEFAULT GETUTCDATE(),
    CONSTRAINT PK_ACCOUNT PRIMARY KEY CLUSTERED (account_id)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX IX_ACCOUNT_username ON dbo.ACCOUNT(username);
CREATE UNIQUE NONCLUSTERED INDEX IX_ACCOUNT_email
    ON dbo.ACCOUNT(email) WHERE email IS NOT NULL;
GO

-- 2. MEMBER: Hồ sơ khách hàng hội viên (1-1 nullable với ACCOUNT)
CREATE TABLE dbo.MEMBER (
    member_id         INT IDENTITY(1,1) NOT NULL,
    account_id        INT NULL,
    full_name         NVARCHAR(255) NOT NULL,
    face_path         NVARCHAR(500) NULL,
    face_vector       NVARCHAR(MAX) NULL,
    spending_limit    DECIMAL(12,2) NULL,
    warning_threshold DECIMAL(12,2) NULL,
    total_points      INT           NOT NULL CONSTRAINT DF_MEMBER_total_points DEFAULT 0,
    CONSTRAINT PK_MEMBER PRIMARY KEY CLUSTERED (member_id),
    CONSTRAINT FK_MEMBER_ACCOUNT FOREIGN KEY (account_id)
        REFERENCES dbo.ACCOUNT(account_id) ON DELETE SET NULL
);
GO

-- 3. MEMBERSHIP: Cấp bậc thành viên (Gold/Silver/Bronze) + điểm tích lũy
CREATE TABLE dbo.MEMBERSHIP (
    membership_id    INT IDENTITY(1,1) NOT NULL,
    member_id        INT NOT NULL,
    tier_name        NVARCHAR(20)  NOT NULL,
    points_threshold INT NOT NULL,
    CONSTRAINT PK_MEMBERSHIP PRIMARY KEY CLUSTERED (membership_id),
    CONSTRAINT FK_MEMBERSHIP_MEMBER FOREIGN KEY (member_id)
        REFERENCES dbo.MEMBER(member_id) ON DELETE CASCADE
);
GO

CREATE UNIQUE NONCLUSTERED INDEX IX_MEMBERSHIP_member_tier
    ON dbo.MEMBERSHIP(member_id, tier_name);
GO

-- 4. HEALTH_TAG: Nhãn sức khoẻ / dinh dưỡng (lưu ý ERD dùng HealthTagID, không phải TagID)
CREATE TABLE dbo.HEALTH_TAG (
    health_tag_id INT IDENTITY(1,1) NOT NULL,
    tag_name      NVARCHAR(50)  NOT NULL,
    tag_type      NVARCHAR(20)  NOT NULL,                                  -- 'diet' | 'allergy' | 'lifestyle'
    CONSTRAINT PK_HEALTH_TAG PRIMARY KEY CLUSTERED (health_tag_id)
);
GO

-- 5. MEMBERHEALTH_PREFERENCE: N-N giữa MEMBER ↔ HEALTH_TAG (có IsAllergy)
CREATE TABLE dbo.MEMBERHEALTH_PREFERENCE (
    member_id     INT NOT NULL,
    health_tag_id INT NOT NULL,
    is_allergy    BIT NOT NULL CONSTRAINT DF_MEMBERHEALTH_PREFERENCE_is_allergy DEFAULT 0,
    CONSTRAINT PK_MEMBERHEALTH_PREFERENCE PRIMARY KEY CLUSTERED (member_id, health_tag_id),
    CONSTRAINT FK_MEMBERHEALTH_PREFERENCE_MEMBER FOREIGN KEY (member_id)
        REFERENCES dbo.MEMBER(member_id) ON DELETE CASCADE,
    CONSTRAINT FK_MEMBERHEALTH_PREFERENCE_HEALTH_TAG FOREIGN KEY (health_tag_id)
        REFERENCES dbo.HEALTH_TAG(health_tag_id) ON DELETE CASCADE
);
GO

-- ─────────────────────────────────────────────────────
-- REGION 2: PRODUCT CATALOG (5 Tables)
-- ─────────────────────────────────────────────────────

-- 6. CATEGORY
CREATE TABLE dbo.CATEGORY (
    category_id   INT IDENTITY(1,1) NOT NULL,
    category_name NVARCHAR(100) NOT NULL,
    description   NVARCHAR(500) NULL,
    CONSTRAINT PK_CATEGORY PRIMARY KEY CLUSTERED (category_id)
);
GO

-- 7. SUBCATEGORY
CREATE TABLE dbo.SUBCATEGORY (
    subcategory_id   INT IDENTITY(1,1) NOT NULL,
    category_id      INT NOT NULL,
    subcategory_name NVARCHAR(100) NOT NULL,
    CONSTRAINT PK_SUBCATEGORY PRIMARY KEY CLUSTERED (subcategory_id),
    CONSTRAINT FK_SUBCATEGORY_CATEGORY FOREIGN KEY (category_id)
        REFERENCES dbo.CATEGORY(category_id)
);
GO

-- 8. PRODUCT_TYPE
CREATE TABLE dbo.PRODUCT_TYPE (
    product_type_id INT IDENTITY(1,1) NOT NULL,
    subcategory_id  INT NOT NULL,
    type_name       NVARCHAR(100) NOT NULL,
    CONSTRAINT PK_PRODUCT_TYPE PRIMARY KEY CLUSTERED (product_type_id),
    CONSTRAINT FK_PRODUCT_TYPE_SUBCATEGORY FOREIGN KEY (subcategory_id)
        REFERENCES dbo.SUBCATEGORY(subcategory_id)
);
GO

-- 9. PRODUCT (có Barcode unique + SubstituteProductID self-ref)
CREATE TABLE dbo.PRODUCT (
    product_id           INT IDENTITY(1,1) NOT NULL,
    product_type_id      INT NOT NULL,
    product_name         NVARCHAR(200)  NOT NULL,
    unit_price           DECIMAL(12,2)  NOT NULL CONSTRAINT DF_PRODUCT_unit_price DEFAULT 0.00,
    barcode              NVARCHAR(50)   NULL,
    image_url            NVARCHAR(500)  NULL,
    weight_or_volume     DECIMAL(10,2)  NULL,
    unit                 NVARCHAR(20)   NULL,                              -- 'g' | 'ml' | 'box' | 'item'
    description          NVARCHAR(1000) NULL,
    is_active            BIT            NOT NULL CONSTRAINT DF_PRODUCT_is_active DEFAULT 1,
    substitute_product_id INT           NULL,
    CONSTRAINT PK_PRODUCT PRIMARY KEY CLUSTERED (product_id),
    CONSTRAINT FK_PRODUCT_PRODUCT_TYPE FOREIGN KEY (product_type_id)
        REFERENCES dbo.PRODUCT_TYPE(product_type_id),
    -- Self-ref: sản phẩm thay thế
    CONSTRAINT FK_PRODUCT_Substitute FOREIGN KEY (substitute_product_id)
        REFERENCES dbo.PRODUCT(product_id) ON DELETE NO ACTION
);
GO

CREATE UNIQUE NONCLUSTERED INDEX IX_PRODUCT_barcode
    ON dbo.PRODUCT(barcode) WHERE barcode IS NOT NULL;
GO

-- 10. PRODUCT_HEALTHTAG: N-N PRODUCTS ↔ HEALTH_TAG (PK ghép - lưu ý ERD: không có cột riêng)
CREATE TABLE dbo.PRODUCT_HEALTHTAG (
    product_id    INT NOT NULL,
    health_tag_id INT NOT NULL,
    CONSTRAINT PK_PRODUCT_HEALTHTAG PRIMARY KEY CLUSTERED (product_id, health_tag_id),
    CONSTRAINT FK_PRODUCT_HEALTHTAG_PRODUCT FOREIGN KEY (product_id)
        REFERENCES dbo.PRODUCT(product_id) ON DELETE CASCADE,
    CONSTRAINT FK_PRODUCT_HEALTHTAG_HEALTH_TAG FOREIGN KEY (health_tag_id)
        REFERENCES dbo.HEALTH_TAG(health_tag_id) ON DELETE CASCADE
);
GO

-- ─────────────────────────────────────────────────────
-- REGION 3: SHOPPING & MEAL (4 Tables)
-- ─────────────────────────────────────────────────────

-- 11. INVOICE_HISTORY: Hoá đơn mua sắm (đổi tên từ ShoppingHistories)
CREATE TABLE dbo.INVOICE_HISTORY (
    invoice_history_id INT IDENTITY(1,1) NOT NULL,
    member_id          INT NOT NULL,
    purchase_date      DATETIME2     NOT NULL CONSTRAINT DF_INVOICE_HISTORY_purchase_date DEFAULT GETUTCDATE(),
    total_amount       DECIMAL(12,2) NOT NULL CONSTRAINT DF_INVOICE_HISTORY_total_amount  DEFAULT 0.00,
    CONSTRAINT PK_INVOICE_HISTORY PRIMARY KEY CLUSTERED (invoice_history_id),
    CONSTRAINT FK_INVOICE_HISTORY_MEMBER FOREIGN KEY (member_id)
        REFERENCES dbo.MEMBER(member_id)
);
GO

-- 12. INVOICE_HISTORY_ITEM: Chi tiết sản phẩm trong hoá đơn
CREATE TABLE dbo.INVOICE_HISTORY_ITEM (
    invoice_history_item_id INT IDENTITY(1,1) NOT NULL,
    invoice_history_id      INT NOT NULL,
    product_id              INT NOT NULL,
    quantity                INT           NOT NULL CONSTRAINT DF_INVOICE_HISTORY_ITEM_quantity  DEFAULT 1,
    unit_price              DECIMAL(12,2) NOT NULL CONSTRAINT DF_INVOICE_HISTORY_ITEM_unit_price DEFAULT 0.00,
    CONSTRAINT PK_INVOICE_HISTORY_ITEM PRIMARY KEY CLUSTERED (invoice_history_item_id),
    CONSTRAINT FK_INVOICE_HISTORY_ITEM_INVOICE FOREIGN KEY (invoice_history_id)
        REFERENCES dbo.INVOICE_HISTORY(invoice_history_id) ON DELETE CASCADE,
    CONSTRAINT FK_INVOICE_HISTORY_ITEM_PRODUCT FOREIGN KEY (product_id)
        REFERENCES dbo.PRODUCT(product_id)
);
GO

-- 13. MEAL_SUGGESTION: Công thức nấu ăn (gợi ý cho hội viên)
CREATE TABLE dbo.MEAL_SUGGESTION (
    meal_suggestion_id     INT IDENTITY(1,1) NOT NULL,
    meal_name              NVARCHAR(200) NOT NULL,
    description            NVARCHAR(MAX) NULL,
    yield_portions         INT           NOT NULL CONSTRAINT DF_MEAL_SUGGESTION_yield_portions DEFAULT 1,
    image_url              NVARCHAR(500) NULL,
    calories               INT           NULL,
    healthy_score          INT           NULL,                              -- 1-100
    alternative_suggestion NVARCHAR(500) NULL,
    CONSTRAINT PK_MEAL_SUGGESTION PRIMARY KEY CLUSTERED (meal_suggestion_id)
);
GO

-- 14. MEAL_ITEM: Nguyên liệu (N-N MEAL_SUGGESTION ↔ PRODUCT, PK ghép)
CREATE TABLE dbo.MEAL_ITEM (
    meal_suggestion_id INT NOT NULL,
    product_id         INT NOT NULL,
    quantity_required  DECIMAL(10,2) NOT NULL,
    unit_of_measure    NVARCHAR(20)  NOT NULL,                              -- 'g' | 'ml' | 'quả' | 'muỗng'
    CONSTRAINT PK_MEAL_ITEM PRIMARY KEY CLUSTERED (meal_suggestion_id, product_id),
    CONSTRAINT FK_MEAL_ITEM_MEAL_SUGGESTION FOREIGN KEY (meal_suggestion_id)
        REFERENCES dbo.MEAL_SUGGESTION(meal_suggestion_id) ON DELETE CASCADE,
    CONSTRAINT FK_MEAL_ITEM_PRODUCT FOREIGN KEY (product_id)
        REFERENCES dbo.PRODUCT(product_id)
);
GO

-- ─────────────────────────────────────────────────────
-- REGION 4: STORE LAYOUT (5 Tables: FLOOR → ZONE → AISLE → SHELF → SLOT)
-- ─────────────────────────────────────────────────────

-- 15. FLOOR
CREATE TABLE dbo.FLOOR (
    floor_id     INT IDENTITY(1,1) NOT NULL,
    floor_number INT NOT NULL,
    CONSTRAINT PK_FLOOR PRIMARY KEY CLUSTERED (floor_id)
);
GO

-- 16. ZONE
CREATE TABLE dbo.ZONE (
    zone_id     INT IDENTITY(1,1) NOT NULL,
    floor_id    INT NOT NULL,
    zone_code   CHAR(1)       NOT NULL,                                    -- 'A', 'B', 'C', 'D', 'E'
    zone_name   NVARCHAR(100) NOT NULL,
    description NVARCHAR(500) NULL,
    is_blocked  BIT           NOT NULL CONSTRAINT DF_ZONE_is_blocked DEFAULT 0,
    CONSTRAINT PK_ZONE PRIMARY KEY CLUSTERED (zone_id),
    CONSTRAINT FK_ZONE_FLOOR FOREIGN KEY (floor_id)
        REFERENCES dbo.FLOOR(floor_id)
);
GO

-- 17. AISLE
CREATE TABLE dbo.AISLE (
    aisle_id     INT IDENTITY(1,1) NOT NULL,
    zone_id      INT NOT NULL,
    aisle_code   NVARCHAR(10)  NOT NULL,                                    -- 'A1', 'A2', 'B1', 'B2'
    aisle_name   NVARCHAR(100) NULL,
    is_blocked   BIT           NOT NULL CONSTRAINT DF_AISLE_is_blocked DEFAULT 0,
    CONSTRAINT PK_AISLE PRIMARY KEY CLUSTERED (aisle_id),
    CONSTRAINT FK_AISLE_ZONE FOREIGN KEY (zone_id)
        REFERENCES dbo.ZONE(zone_id)
);
GO

-- 18. SHELF (Tầng kệ - tách riêng theo ERD mới)
CREATE TABLE dbo.SHELF (
    shelf_id     INT IDENTITY(1,1) NOT NULL,
    aisle_id     INT NOT NULL,
    level_number INT NOT NULL,                                              -- 1: sát đất, 3: tầm mắt, 5: trên cùng
    CONSTRAINT PK_SHELF PRIMARY KEY CLUSTERED (shelf_id),
    CONSTRAINT FK_SHELF_AISLE FOREIGN KEY (aisle_id)
        REFERENCES dbo.AISLE(aisle_id)
);
GO

-- 19. SLOT (Ô trưng bày - theo ERD: Quantity + ExpiryDate + Supplier, không có ProductID trực tiếp)
CREATE TABLE dbo.SLOT (
    slot_id         INT IDENTITY(1,1) NOT NULL,
    shelf_id        INT NOT NULL,
    slot_code       NVARCHAR(10)  NOT NULL,                                -- '01', '02', '03'
    quantity        INT           NOT NULL CONSTRAINT DF_SLOT_quantity DEFAULT 0,
    expiry_date     DATE          NULL,                                    -- HSD lô hàng trong ô
    supplier        NVARCHAR(200) NULL,                                    -- Nhà cung cấp cụ thể
    last_scanned_at DATETIME2     NULL,
    CONSTRAINT PK_SLOT PRIMARY KEY CLUSTERED (slot_id),
    CONSTRAINT FK_SLOT_SHELF FOREIGN KEY (shelf_id)
        REFERENCES dbo.SHELF(shelf_id)
);
GO

-- 20. PRODUCT_SLOT: N-N PRODUCT ↔ SLOT (theo ERD mới - tách riêng)
CREATE TABLE dbo.PRODUCT_SLOT (
    product_slot_id INT IDENTITY(1,1) NOT NULL,
    slot_id         INT NOT NULL,
    product_id      INT NOT NULL,
    CONSTRAINT PK_PRODUCT_SLOT PRIMARY KEY CLUSTERED (product_slot_id),
    CONSTRAINT FK_PRODUCT_SLOT_SLOT FOREIGN KEY (slot_id)
        REFERENCES dbo.SLOT(slot_id) ON DELETE CASCADE,
    CONSTRAINT FK_PRODUCT_SLOT_PRODUCT FOREIGN KEY (product_id)
        REFERENCES dbo.PRODUCT(product_id) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX IX_PRODUCT_SLOT_slot_product
    ON dbo.PRODUCT_SLOT(slot_id, product_id);
GO

-- ─────────────────────────────────────────────────────
-- REGION 5: AD & SPONSORSHIP (4 Tables)
-- ─────────────────────────────────────────────────────

-- 21. BRAND
CREATE TABLE dbo.BRAND (
    brand_id    INT IDENTITY(1,1) NOT NULL,
    brand_name  NVARCHAR(100) NOT NULL,
    description NVARCHAR(500) NULL,
    CONSTRAINT PK_BRAND PRIMARY KEY CLUSTERED (brand_id)
);
GO

-- 22. AD_PACKAGE
CREATE TABLE dbo.AD_PACKAGE (
    package_id     INT IDENTITY(1,1) NOT NULL,
    package_name   NVARCHAR(100)  NOT NULL,
    price          DECIMAL(18,2)  NOT NULL CONSTRAINT DF_AD_PACKAGE_price          DEFAULT 0.00,
    ad_score       INT            NOT NULL CONSTRAINT DF_AD_PACKAGE_ad_score      DEFAULT 0,
    is_weekend_only BIT           NOT NULL CONSTRAINT DF_AD_PACKAGE_is_weekend_only DEFAULT 0,
    CONSTRAINT PK_AD_PACKAGE PRIMARY KEY CLUSTERED (package_id)
);
GO

-- 23. AD_CAMPAIGN
CREATE TABLE dbo.AD_CAMPAIGN (
    ad_campaign_id INT IDENTITY(1,1) NOT NULL,
    package_id     INT NOT NULL,
    brand_id       INT NOT NULL,
    campaign_name  NVARCHAR(200) NOT NULL,
    start_date     DATETIME2 NOT NULL,
    end_date       DATETIME2 NOT NULL,
    is_active      BIT        NOT NULL CONSTRAINT DF_AD_CAMPAIGN_is_active DEFAULT 1,
    CONSTRAINT PK_AD_CAMPAIGN PRIMARY KEY CLUSTERED (ad_campaign_id),
    CONSTRAINT FK_AD_CAMPAIGN_AD_PACKAGE FOREIGN KEY (package_id)
        REFERENCES dbo.AD_PACKAGE(package_id) ON DELETE NO ACTION,
    CONSTRAINT FK_AD_CAMPAIGN_BRAND FOREIGN KEY (brand_id)
        REFERENCES dbo.BRAND(brand_id) ON DELETE NO ACTION
);
GO

-- 24. SPONSORED_PRODUCT
CREATE TABLE dbo.SPONSORED_PRODUCT (
    sponsored_id    INT IDENTITY(1,1) NOT NULL,
    ad_campaign_id  INT NOT NULL,
    product_id      INT NOT NULL,
    brand_id        INT NOT NULL,
    priority        INT NOT NULL CONSTRAINT DF_SPONSORED_PRODUCT_priority DEFAULT 0,
    is_active       BIT NOT NULL CONSTRAINT DF_SPONSORED_PRODUCT_is_active DEFAULT 1,
    CONSTRAINT PK_SPONSORED_PRODUCT PRIMARY KEY CLUSTERED (sponsored_id),
    CONSTRAINT FK_SPONSORED_PRODUCT_AD_CAMPAIGN FOREIGN KEY (ad_campaign_id)
        REFERENCES dbo.AD_CAMPAIGN(ad_campaign_id) ON DELETE CASCADE,
    CONSTRAINT FK_SPONSORED_PRODUCT_PRODUCT FOREIGN KEY (product_id)
        REFERENCES dbo.PRODUCT(product_id) ON DELETE CASCADE,
    CONSTRAINT FK_SPONSORED_PRODUCT_BRAND FOREIGN KEY (brand_id)
        REFERENCES dbo.BRAND(brand_id) ON DELETE NO ACTION
);
GO

-- 25. AD_CAMPAIGN_LOG: Nhật ký chiến dịch quảng cáo
CREATE TABLE dbo.AD_CAMPAIGN_LOG (
    log_id          INT IDENTITY(1,1) NOT NULL,
    ad_campaign_id  INT NOT NULL,
    action_type     NVARCHAR(50) NOT NULL,                                  -- 'Impression' | 'Click' | 'Conversion' | 'Created' | 'Paused'
    timestamp       DATETIME2    NOT NULL CONSTRAINT DF_AD_CAMPAIGN_LOG_timestamp DEFAULT GETUTCDATE(),
    CONSTRAINT PK_AD_CAMPAIGN_LOG PRIMARY KEY CLUSTERED (log_id),
    CONSTRAINT FK_AD_CAMPAIGN_LOG_AD_CAMPAIGN FOREIGN KEY (ad_campaign_id)
        REFERENCES dbo.AD_CAMPAIGN(ad_campaign_id) ON DELETE CASCADE
);
GO

-- ─────────────────────────────────────────────────────
-- REGION 6: ROBOT & NAVIGATION (10 Tables)
-- ─────────────────────────────────────────────────────

-- 26. ROBOT
CREATE TABLE dbo.ROBOT (
    robot_id    INT IDENTITY(1,1) NOT NULL,
    robot_name  NVARCHAR(100) NOT NULL,
    robot_code  NVARCHAR(50)  NOT NULL,                                    -- 'ROBOT-01' - UNIQUE
    battery_pct INT           NOT NULL CONSTRAINT DF_ROBOT_battery_pct DEFAULT 100,
    mode        NVARCHAR(20)  NOT NULL CONSTRAINT DF_ROBOT_mode        DEFAULT 'idle',
    is_online   BIT           NOT NULL CONSTRAINT DF_ROBOT_is_online   DEFAULT 0,
    last_seen_at DATETIME2    NULL,
    CONSTRAINT PK_ROBOT PRIMARY KEY CLUSTERED (robot_id)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX IX_ROBOT_robot_code ON dbo.ROBOT(robot_code);
GO

-- 27. ROBOT_LOG: Nhật ký telemetry robot (cột lowercase để tương thích MQTT payload)
CREATE TABLE dbo.ROBOT_LOG (
    log_id     INT IDENTITY(1,1) NOT NULL,
    robot_id   INT NULL,
    battery    INT NULL,                                                  -- trùng MQTT payload
    location   NVARCHAR(255) NULL,
    status     NVARCHAR(100) NULL,
    timestamp  DATETIME2 NOT NULL CONSTRAINT DF_ROBOT_LOG_timestamp DEFAULT SYSUTCDATETIME(),
    x_coord    FLOAT NULL,
    y_coord    FLOAT NULL,
    heading_rad FLOAT NULL,
    CONSTRAINT PK_ROBOT_LOG PRIMARY KEY CLUSTERED (log_id),
    CONSTRAINT FK_ROBOT_LOG_ROBOT FOREIGN KEY (robot_id)
        REFERENCES dbo.ROBOT(robot_id) ON DELETE SET NULL
);
GO

CREATE NONCLUSTERED INDEX IX_ROBOT_LOG_robot_timestamp
    ON dbo.ROBOT_LOG(robot_id, timestamp DESC);
GO

-- 28. ROBOT_ZONE: N-N ROBOT ↔ ZONE
CREATE TABLE dbo.ROBOT_ZONE (
    robot_zone_id INT IDENTITY(1,1) NOT NULL,
    robot_id      INT NOT NULL,
    zone_id       INT NOT NULL,
    CONSTRAINT PK_ROBOT_ZONE PRIMARY KEY CLUSTERED (robot_zone_id),
    CONSTRAINT FK_ROBOT_ZONE_ROBOT FOREIGN KEY (robot_id)
        REFERENCES dbo.ROBOT(robot_id) ON DELETE CASCADE,
    CONSTRAINT FK_ROBOT_ZONE_ZONE FOREIGN KEY (zone_id)
        REFERENCES dbo.ZONE(zone_id)
);
GO

-- 29. MAP
CREATE TABLE dbo.MAP (
    map_id     INT IDENTITY(1,1) NOT NULL,
    floor_id   INT NOT NULL,
    map_name   NVARCHAR(100) NOT NULL,
    map_data   NVARCHAR(MAX) NULL,                                         -- JSON toạ độ vật cản tĩnh
    created_at DATETIME2     NOT NULL CONSTRAINT DF_MAP_created_at DEFAULT GETUTCDATE(),
    CONSTRAINT PK_MAP PRIMARY KEY CLUSTERED (map_id),
    CONSTRAINT FK_MAP_FLOOR FOREIGN KEY (floor_id)
        REFERENCES dbo.FLOOR(floor_id)
);
GO

-- 30. NAVIGATION_NODE (NodeType không có 'station' theo ERD mới)
CREATE TABLE dbo.NAVIGATION_NODE (
    node_id     INT IDENTITY(1,1) NOT NULL,
    map_id      INT NOT NULL,
    node_name   NVARCHAR(100) NOT NULL,
    x_coord     FLOAT NOT NULL,                                              -- mét
    y_coord     FLOAT NOT NULL,
    node_type   NVARCHAR(20)  NOT NULL,                                      -- 'entrance' | 'shelf_front' | 'intersection'
    is_blocked  BIT           NOT NULL CONSTRAINT DF_NAVIGATION_NODE_is_blocked DEFAULT 0,
    CONSTRAINT PK_NAVIGATION_NODE PRIMARY KEY CLUSTERED (node_id),
    CONSTRAINT FK_NAVIGATION_NODE_MAP FOREIGN KEY (map_id)
        REFERENCES dbo.MAP(map_id) ON DELETE CASCADE
);
GO

-- 31. NAVIGATION_EDGE
CREATE TABLE dbo.NAVIGATION_EDGE (
    edge_id          INT IDENTITY(1,1) NOT NULL,
    from_node_id     INT NOT NULL,
    to_node_id       INT NOT NULL,
    distance         FLOAT NOT NULL,                                          -- mét
    is_bidirectional BIT   NOT NULL CONSTRAINT DF_NAVIGATION_EDGE_is_bidirectional DEFAULT 1,
    CONSTRAINT PK_NAVIGATION_EDGE PRIMARY KEY CLUSTERED (edge_id),
    CONSTRAINT FK_NAVIGATION_EDGE_From FOREIGN KEY (from_node_id)
        REFERENCES dbo.NAVIGATION_NODE(node_id) ON DELETE NO ACTION,
    CONSTRAINT FK_NAVIGATION_EDGE_To   FOREIGN KEY (to_node_id)
        REFERENCES dbo.NAVIGATION_NODE(node_id) ON DELETE NO ACTION
);
GO

-- 32. AISLE_NODE: N-N AISLE ↔ NAVIGATION_NODE (theo ERD mới)
CREATE TABLE dbo.AISLE_NODE (
    aisle_node_id INT IDENTITY(1,1) NOT NULL,
    aisle_id      INT NOT NULL,
    node_id       INT NOT NULL,
    CONSTRAINT PK_AISLE_NODE PRIMARY KEY CLUSTERED (aisle_node_id),
    CONSTRAINT FK_AISLE_NODE_AISLE FOREIGN KEY (aisle_id)
        REFERENCES dbo.AISLE(aisle_id) ON DELETE CASCADE,
    CONSTRAINT FK_AISLE_NODE_NAVIGATION_NODE FOREIGN KEY (node_id)
        REFERENCES dbo.NAVIGATION_NODE(node_id) ON DELETE CASCADE
);
GO

-- 33. ROBOT_ROUTE: Lộ trình đã lên kế hoạch (Dijkstra output)
CREATE TABLE dbo.ROBOT_ROUTE (
    robot_route_id INT IDENTITY(1,1) NOT NULL,
    robot_id       INT NOT NULL,
    map_id         INT NOT NULL,
    route_name     NVARCHAR(200) NOT NULL,
    created_at     DATETIME2     NOT NULL CONSTRAINT DF_ROBOT_ROUTE_created_at DEFAULT GETUTCDATE(),
    CONSTRAINT PK_ROBOT_ROUTE PRIMARY KEY CLUSTERED (robot_route_id),
    CONSTRAINT FK_ROBOT_ROUTE_ROBOT FOREIGN KEY (robot_id)
        REFERENCES dbo.ROBOT(robot_id) ON DELETE CASCADE,
    CONSTRAINT FK_ROBOT_ROUTE_MAP FOREIGN KEY (map_id)
        REFERENCES dbo.MAP(map_id)
);
GO

-- 34. ROUTE_NODE_MAPPING: Chi tiết thứ tự node trong route
CREATE TABLE dbo.ROUTE_NODE_MAPPING (
    route_node_mapping_id INT IDENTITY(1,1) NOT NULL,
    robot_route_id        INT NOT NULL,
    node_id               INT NOT NULL,
    sequence_order        INT NOT NULL,                                       -- 1, 2, 3...
    CONSTRAINT PK_ROUTE_NODE_MAPPING PRIMARY KEY CLUSTERED (route_node_mapping_id),
    CONSTRAINT FK_ROUTE_NODE_MAPPING_ROBOT_ROUTE FOREIGN KEY (robot_route_id)
        REFERENCES dbo.ROBOT_ROUTE(robot_route_id) ON DELETE CASCADE,
    CONSTRAINT FK_ROUTE_NODE_MAPPING_NAVIGATION_NODE FOREIGN KEY (node_id)
        REFERENCES dbo.NAVIGATION_NODE(node_id) ON DELETE NO ACTION
);
GO

-- 35. ROUTE_ASSIGNMENT: Gán route cho robot (nhiều assignment theo thời gian)
CREATE TABLE dbo.ROUTE_ASSIGNMENT (
    route_assignment_id INT IDENTITY(1,1) NOT NULL,
    robot_id            INT NOT NULL,
    robot_route_id      INT NOT NULL,
    assigned_at         DATETIME2 NOT NULL CONSTRAINT DF_ROUTE_ASSIGNMENT_assigned_at DEFAULT GETUTCDATE(),
    status              NVARCHAR(50) NOT NULL,                                 -- 'Pending' | 'InProgress' | 'Completed' | 'Failed'
    CONSTRAINT PK_ROUTE_ASSIGNMENT PRIMARY KEY CLUSTERED (route_assignment_id),
    CONSTRAINT FK_ROUTE_ASSIGNMENT_ROBOT FOREIGN KEY (robot_id)
        REFERENCES dbo.ROBOT(robot_id) ON DELETE CASCADE,
    CONSTRAINT FK_ROUTE_ASSIGNMENT_ROBOT_ROUTE FOREIGN KEY (robot_route_id)
        REFERENCES dbo.ROBOT_ROUTE(robot_route_id) ON DELETE NO ACTION
);
GO

-- 36. AISLE_SCAN: Lịch sử quét kệ hàng (Gemini AI Vision) - computed NeedsRestock
CREATE TABLE dbo.AISLE_SCAN (
    scan_id          INT IDENTITY(1,1) NOT NULL,
    aisle_id         INT NOT NULL,
    robot_id         INT NOT NULL,
    scanned_at       DATETIME2     NOT NULL CONSTRAINT DF_AISLE_SCAN_scanned_at       DEFAULT GETUTCDATE(),
    image_url        NVARCHAR(500) NULL,                                       -- Azure Blob Storage
    empty_percentage DECIMAL(5,2)  NOT NULL CONSTRAINT DF_AISLE_SCAN_empty_percentage DEFAULT 0.00,
    needs_restock AS CAST(CASE WHEN empty_percentage > 30.0 THEN 1 ELSE 0 END AS BIT),  -- Computed
    ai_response_raw  NVARCHAR(MAX) NULL,                                       -- JSON thô từ Gemini
    CONSTRAINT PK_AISLE_SCAN PRIMARY KEY CLUSTERED (scan_id),
    CONSTRAINT FK_AISLE_SCAN_AISLE FOREIGN KEY (aisle_id)
        REFERENCES dbo.AISLE(aisle_id),
    CONSTRAINT FK_AISLE_SCAN_ROBOT FOREIGN KEY (robot_id)
        REFERENCES dbo.ROBOT(robot_id)
);
GO

-- 37. SEMANTIC_OBJECT: Vật thể tĩnh vẽ trên bản đồ (AABB bounding box)
CREATE TABLE dbo.SEMANTIC_OBJECT (
    object_id   INT IDENTITY(1,1) NOT NULL,
    map_id      INT NOT NULL,
    object_type NVARCHAR(50) NOT NULL,                                         -- 'shelf' | 'wall' | 'door' | 'obstacle'
    x_min       FLOAT NOT NULL,
    y_min       FLOAT NOT NULL,
    x_max       FLOAT NOT NULL,
    y_max       FLOAT NOT NULL,
    label       NVARCHAR(100) NULL,                                              -- nhãn AI detect
    confidence  DECIMAL(5,2) NULL,                                               -- 0.00 - 1.00
    detected_at DATETIME2    NULL,
    image_url   NVARCHAR(500) NULL,
    CONSTRAINT PK_SEMANTIC_OBJECT PRIMARY KEY CLUSTERED (object_id),
    CONSTRAINT FK_SEMANTIC_OBJECT_MAP FOREIGN KEY (map_id)
        REFERENCES dbo.MAP(map_id) ON DELETE CASCADE
);
GO

PRINT N'   - Đã tạo 37 bảng theo ERD V4.0';
PRINT N'   - 1 computed column: AISLE_SCAN.needs_restock';
GO

-- ============================================================================
-- PHẦN 4: TẠO 4 VIEW TƯƠNG THÍCH NGƯỤC 100% CHO AI / n8n
-- ============================================================================
-- Lưu ý: View giữ nguyên contract tên cột cũ (PascalCase) để AI FastAPI & n8n
-- workflow không cần đổi code. Bên trong view query theo schema mới (UPPER_CASE).
-- ============================================================================
PRINT N'👁️  PHẦN 4: Tạo 4 view tương thích AI/n8n...';
GO

-- 4.1. PurchaseHistory - TOP sản phẩm mua gần đây (Face Login AI)
CREATE VIEW dbo.PurchaseHistory AS
SELECT
    ihi.invoice_history_item_id AS PurchaseID,
    ih.member_id                AS MemberID,
    p.product_name              AS ProductName,
    ih.purchase_date            AS PurchaseDate
FROM dbo.INVOICE_HISTORY_ITEM ihi
INNER JOIN dbo.INVOICE_HISTORY ih ON ihi.invoice_history_id = ih.invoice_history_id
INNER JOIN dbo.PRODUCT          p  ON ihi.product_id         = p.product_id;
GO

-- 4.2. Store_Map - Vị trí kệ hàng cho LangChain Navigation Agent
CREATE VIEW dbo.Store_Map AS
SELECT
    p.product_id   AS MapID,
    p.product_name AS ProductName,
    ISNULL(N'Kệ ' + a.aisle_code, N'Chưa xếp kệ') AS ShelfLocation,
    ISNULL(z.zone_name, N'Khu vực chung')            AS Landmark,
    ISNULL(a.aisle_name, N'')                        AS AisleNote
FROM dbo.PRODUCT p
LEFT JOIN dbo.PRODUCT_SLOT  ps  ON p.product_id   = ps.product_id
LEFT JOIN dbo.SLOT          s   ON ps.slot_id     = s.slot_id
LEFT JOIN dbo.SHELF         sh  ON s.shelf_id     = sh.shelf_id
LEFT JOIN dbo.AISLE         a   ON sh.aisle_id    = a.aisle_id
LEFT JOIN dbo.ZONE          z   ON a.zone_id      = z.zone_id;
GO

-- 4.3. Blocked_Aisles - Dãy hàng bị chặn robot
CREATE VIEW dbo.Blocked_Aisles AS
SELECT
    aisle_id    AS AisleID,
    aisle_code  AS AisleCode,
    is_blocked  AS IsBlocked,
    CAST(NULL AS NVARCHAR(255)) AS Reason                                -- ERD mới không có BlockReason
FROM dbo.AISLE;
GO

-- 4.4. Real_Time_Stock - Tồn kho + sản phẩm thay thế (qua PRODUCT_SLOT → SLOT)
CREATE VIEW dbo.Real_Time_Stock AS
SELECT
    p.product_id   AS StockID,
    p.product_name AS ProductName,
    ISNULL((SELECT SUM(ps_slot.quantity)
            FROM dbo.PRODUCT_SLOT ps_l
            INNER JOIN dbo.SLOT ps_slot ON ps_l.slot_id = ps_slot.slot_id
            WHERE ps_l.product_id = p.product_id), 0) AS StockQuantity,
    sub.product_name AS SubstituteProduct
FROM dbo.PRODUCT p
LEFT JOIN dbo.PRODUCT sub ON p.substitute_product_id = sub.product_id;
GO

PRINT N'   - Đã tạo 4 view (PurchaseHistory, Store_Map, Blocked_Aisles, Real_Time_Stock)';
GO

-- ============================================================================
-- PHẦN 5: SEED DATA (SIÊU THỊ VIỆT NAM MẪU) - theo schema V4.0
-- ============================================================================
PRINT N'🌱 PHẦN 5: Seed data mẫu...';
GO

-- 5.1. Tài khoản hệ thống (Role theo ERD: 'Admin' | 'Staff' | 'Member')
SET IDENTITY_INSERT dbo.ACCOUNT ON;
INSERT INTO dbo.ACCOUNT (account_id, username, password_hash, email, phone, full_name, is_active, role) VALUES
(1, 'admin_lth',    'hash_pbkdf2_code_123', 'hieultse161727@fpt.edu.vn', '0986515253', N'Lê Tiến Hiếu',      1, 'Admin'),
(2, 'member_qhuy',  'hash_pbkdf2_code_123', 'huynqse160498@fpt.edu.vn',  '0782766322', N'Nguyễn Quang Huy',  1, 'Member'),
(3, 'member_ahung', 'hash_pbkdf2_code_123', 'hungnase180159@fpt.ecu.vn', '0868205403', N'Nguyễn Anh Hùng',   1, 'Member'),
(4, 'staff_dtnhan', 'hash_pbkdf2_code_123', 'nhandt35@fe.edu.vn',        '0903056041', N'Đỗ Tấn Nhàn',      1, 'Staff');
SET IDENTITY_INSERT dbo.ACCOUNT OFF;
GO

-- 5.2. Hồ sơ Member (Hùng & Huy dùng khuôn mặt giả lập để AI quét khớp)
SET IDENTITY_INSERT dbo.MEMBER ON;
INSERT INTO dbo.MEMBER (member_id, account_id, full_name, face_path, face_vector, spending_limit, warning_threshold, total_points) VALUES
(1, 2, N'Nguyễn Quang Huy', N'/storage/faces/huy_nq.jpg',  N'[0.015, -0.042, 0.125, -0.098]',  200000.00, 180000.00, 1500),
(2, 3, N'Nguyễn Anh Hùng',  N'/storage/faces/hung_na.jpg', N'[-0.032, 0.088, 0.054, 0.112]',  150000.00, 130000.00,  800);
SET IDENTITY_INSERT dbo.MEMBER OFF;
GO

-- 5.3. Membership (cấp bậc + ngưỡng điểm)
SET IDENTITY_INSERT dbo.MEMBERSHIP ON;
INSERT INTO dbo.MEMBERSHIP (membership_id, member_id, tier_name, points_threshold) VALUES
(1, 1, N'Gold',   1000),
(2, 2, N'Silver',  500);
SET IDENTITY_INSERT dbo.MEMBERSHIP OFF;
GO

-- 5.4. Health Tag (theo ERD mới dùng health_tag_id)
SET IDENTITY_INSERT dbo.HEALTH_TAG ON;
INSERT INTO dbo.HEALTH_TAG (health_tag_id, tag_name, tag_type) VALUES
(1, N'Không đường',        'diet'),
(2, N'Ít béo',             'diet'),
(3, N'Thuần chay (Vegan)', 'diet'),
(4, N'Organic Hữu cơ',     'diet'),
(5, N'Dị ứng sữa',         'allergy'),
(6, N'Dị ứng hạt lạc',     'allergy'),
(7, N'Dị ứng hải sản',     'allergy');
SET IDENTITY_INSERT dbo.HEALTH_TAG OFF;
GO

-- 5.5. MemberHealthPreference (gắn nhãn cho Huy + Hùng)
INSERT INTO dbo.MEMBERHEALTH_PREFERENCE (member_id, health_tag_id, is_allergy) VALUES
(1, 1, 0),  -- Huy thích Không đường
(1, 4, 0),  -- Huy thích Organic
(2, 6, 1);  -- Hùng bị dị ứng hạt lạc
GO

-- 5.6. Cấu trúc siêu thị (1 FLOOR → 5 ZONE → 10 AISLE → 30 SHELF → 15 SLOT)
INSERT INTO dbo.FLOOR (floor_number) VALUES (1);
GO

SET IDENTITY_INSERT dbo.ZONE ON;
INSERT INTO dbo.ZONE (zone_id, floor_id, zone_code, zone_name, description) VALUES
(1, 1, 'A', N'Thực phẩm tươi sống',     N'Khu bán hoa quả, rau củ, thịt cá tươi sống'),
(2, 1, 'B', N'Đồ uống & Giải khát',    N'Khu nước ngọt, bia, sữa tươi và trà giải nhiệt'),
(3, 1, 'C', N'Hóa mỹ phẩm & Đồ dùng', N'Khu dầu gội, nước rửa chén, chất tẩy rửa sinh hoạt'),
(4, 1, 'D', N'Bánh kẹo & Đồ ăn vặt',  N'Khu bánh ngọt, snack, kẹo dẻo cho trẻ em'),
(5, 1, 'E', N'Gia vị & Đồ khô',       N'Khu hạt nêm, nước mắm, mì tôm ăn liền');
SET IDENTITY_INSERT dbo.ZONE OFF;
GO

SET IDENTITY_INSERT dbo.AISLE ON;
INSERT INTO dbo.AISLE (aisle_id, zone_id, aisle_code, aisle_name, is_blocked) VALUES
(1,  1, 'A1', N'Dãy trái cây nhập khẩu',         0),
(2,  1, 'A2', N'Dãy rau xanh hữu cơ',           0),
(3,  2, 'B1', N'Kệ nước giải khát & Nước ngọt', 0),
(4,  2, 'B2', N'Kệ sữa tươi & Sữa chua',       0),
(5,  3, 'C1', N'Kệ hóa phẩm vệ sinh gia đình', 0),
(6,  3, 'C2', N'Kệ dầu gội & Sữa tắm',         0),
(7,  4, 'D1', N'Dãy bánh quy & Bánh xốp',       0),
(8,  4, 'D2', N'Dãy khoai tây chiên & Snack',   0),
(9,  5, 'E1', N'Kệ mì ăn liền & Đồ ăn khô',    0),
(10, 5, 'E2', N'Kệ gia vị mắm muối bột ngọt',  0);
SET IDENTITY_INSERT dbo.AISLE OFF;
GO

-- 30 SHELF (3 tầng × 10 aisle)
INSERT INTO dbo.SHELF (aisle_id, level_number)
SELECT aisle_id, n FROM dbo.AISLE
CROSS JOIN (VALUES (1), (2), (3)) AS L(n);
GO

-- 5.7. Catalog (CATEGORY → SUBCATEGORY → PRODUCT_TYPE → PRODUCT)
INSERT INTO dbo.CATEGORY (category_name, description) VALUES
(N'Hàng Tiêu Dùng Nhanh (FMCG)', N'Sản phẩm thiết yếu ăn uống hàng ngày'),
(N'Hóa Mỹ Phẩm & Chăm Sóc',     N'Vật dụng tẩy rửa gia đình và vệ sinh cá nhân');
GO

INSERT INTO dbo.SUBCATEGORY (category_id, subcategory_name) VALUES
(1, N'Nước Giải Khát & Đồ Uống'),
(1, N'Sữa & Sản phẩm từ Sữa'),
(1, N'Mì Ăn Liền & Đồ Khô'),
(1, N'Bánh Kẹo & Đồ Ăn Vặt'),
(2, N'Hóa Phẩm Gia Đình');
GO

INSERT INTO dbo.PRODUCT_TYPE (subcategory_id, type_name) VALUES
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

-- 15 sản phẩm Việt Nam thực tế
SET IDENTITY_INSERT dbo.PRODUCT ON;
INSERT INTO dbo.PRODUCT (product_id, product_type_id, product_name, unit_price, barcode, image_url, weight_or_volume, unit, description) VALUES
(1,  1, N'Nước ngọt Coca Cola lon',                              10000.00, '8935049500015', N'/images/products/coca_cola.jpg',        320.00, 'ml',  N'Nước ngọt giải khát có ga truyền thống hương vị thơm ngon'),
(2,  1, N'Nước ngọt Pepsi lon',                                  9500.00,  '8935049500022', N'/images/products/pepsi.jpg',            320.00, 'ml',  N'Nước giải khát có ga mát lạnh sảng khoái cực đỉnh'),
(3,  1, N'Nước ngọt Coca Cola Không Đường',                     10500.00, '8935049500039', N'/images/products/coca_zero.jpg',        320.00, 'ml',  N'Phiên bản nước ngọt ga không đường tốt cho sức khỏe ăn kiêng'),
(4,  2, N'Trà xanh không độ chai',                              12000.00, '8935049500046', N'/images/products/tra_xanh_0d.jpg',     455.00, 'ml',  N'Trà xanh chiết xuất từ lá trà tươi mát lành, chứa chất chống oxy hóa'),
(5,  3, N'Sữa tươi tiệt trùng TH True Milk ít đường',          38000.00, '8935049500053', N'/images/products/th_it_duong.jpg',     1000.00,'ml',  N'Sữa tươi tiệt trùng làm từ sữa sạch nguyên chất của trang trại TH'),
(6,  3, N'Sữa tươi tiệt trùng Vinamilk Không Đường',            36000.00, '8935049500060', N'/images/products/vnm_khong_duong.jpg', 1000.00,'ml',  N'Sữa tươi nguyên chất tiệt trùng 100% không thêm đường thơm ngon tự nhiên'),
(7,  4, N'Sữa chua ăn Vinamilk có đường hộp',                    8000.00, '8935049500077', N'/images/products/vnm_yogurt.jpg',       100.00, 'g',   N'Sữa chua thơm ngon bổ sung men vi sinh hỗ trợ tiêu hóa tốt'),
(8,  5, N'Mì tôm Hảo Hảo tôm chua cay gói',                     4500.00,  '8935049500084', N'/images/products/hao_hao.jpg',          75.00,  'g',   N'Mì ăn liền quốc dân hương vị tôm chua cay đậm đà khó cưỡng'),
(9,  5, N'Mì trộn Omachi xốt sườn hầm gói',                     8500.00,  '8935049500091', N'/images/products/omachi_suon.jpg',      80.00,  'g',   N'Mì làm từ khoai tây hảo hạng dai ngon kết hợp nước xốt sườn hầm thơm phức'),
(10, 6, N'Gạo ST25 Ông Cua túi cao cấp',                        185000.00,'8935049500107', N'/images/products/gao_st25.jpg',         5.00,   'kg',  N'Gạo đạt giải gạo ngon nhất thế giới, hạt cơm dẻo thơm hương dứa'),
(11, 7, N'Bánh ChocoPie Orion hộp lớn',                          55000.00, '8935049500114', N'/images/products/chocopie.jpg',         396.00, 'g',   N'Bánh socola ngọt ngào hòa quyện cùng lớp kem dẻo marshmallow hấp dẫn'),
(12, 7, N'Bánh trứng Custas Orion hộp',                          48000.00, '8935049500121', N'/images/products/custas.jpg',           141.00, 'g',   N'Bánh bông lan mềm mại nhân kem trứng ngọt ngào, thơm lừng béo ngậy'),
(13, 8, N'Snack khoai tây Lays tự nhiên gói',                    15000.00, '8935049500138', N'/images/products/lays_classic.jpg',     63.00,  'g',   N'Lát khoai tây vàng giòn tẩm ướp muối tinh khiết giòn rụm thơm ngon'),
(14, 8, N'Đậu phộng Tân Tân vị nước cốt dừa',                    18000.00, '8935049500145', N'/images/products/tan_tan_dua.jpg',      100.00, 'g',   N'Hạt đậu phộng giòn bùi kết hợp hương thơm béo ngậy của nước cốt dừa'),
(15, 9, N'Nước rửa chén Sunlight chanh chai',                    32000.00, '8935049500152', N'/images/products/sunlight_chanh.jpg',   750.00, 'ml',  N'Sức mạnh tẩy sạch dầu mỡ siêu tốc từ tinh chất chanh tươi mát rượi');
SET IDENTITY_INSERT dbo.PRODUCT OFF;
GO

-- Sản phẩm thay thế (self-ref)
UPDATE dbo.PRODUCT SET substitute_product_id = 2  WHERE product_id = 1;  -- Coca ↔ Pepsi
UPDATE dbo.PRODUCT SET substitute_product_id = 1  WHERE product_id = 2;
UPDATE dbo.PRODUCT SET substitute_product_id = 6  WHERE product_id = 5;  -- TH → Vinamilk
UPDATE dbo.PRODUCT SET substitute_product_id = 9  WHERE product_id = 8;  -- Hảo Hảo → Omachi
GO

-- Gắn nhãn dinh dưỡng cho sản phẩm
INSERT INTO dbo.PRODUCT_HEALTHTAG (product_id, health_tag_id) VALUES
(3, 1),  -- Coca Không đường
(3, 2),
(6, 1),  -- Sữa Vinamilk Không đường
(6, 2),
(6, 4),
(14, 6); -- Đậu phộng Tân Tân chứa dị ứng hạt lạc
GO

-- 5.8. SLOT (quantity + expiry + supplier, KHÔNG có product_id trực tiếp theo ERD mới)
SET IDENTITY_INSERT dbo.SLOT ON;
INSERT INTO dbo.SLOT (slot_id, shelf_id, slot_code, quantity, expiry_date, supplier, last_scanned_at) VALUES
-- Dãy B1: nước ngọt (Shelf ID 7-9)
(1,  7,  '01', 15, CAST(DATEADD(day, 180, GETUTCDATE()) AS DATE), N'Coca-Cola Việt Nam',        GETUTCDATE()),
(2,  7,  '02', 8,  CAST(DATEADD(day, 180, GETUTCDATE()) AS DATE), N'PepsiCo Việt Nam',          GETUTCDATE()),
(3,  8,  '01', 22, CAST(DATEADD(day, 180, GETUTCDATE()) AS DATE), N'Coca-Cola Việt Nam',        GETUTCDATE()),
(4,  9,  '01', 14, CAST(DATEADD(day, 90,  GETUTCDATE()) AS DATE), N'Unilever Việt Nam',         GETUTCDATE()),
-- Dãy B2: sữa (Shelf ID 10-12)
(5,  10, '01', 10, CAST(DATEADD(day, 30,  GETUTCDATE()) AS DATE), N'TH True Milk',               GETUTCDATE()),
(6,  11, '01', 2,  CAST(DATEADD(day, 30,  GETUTCDATE()) AS DATE), N'Vinamilk',                   GETUTCDATE()),  -- Sắp hết
(7,  12, '01', 30, CAST(DATEADD(day, 15,  GETUTCDATE()) AS DATE), N'Vinamilk',                   GETUTCDATE()),
-- Dãy E1: mì ăn liền (Shelf ID 25-26)
(8,  25, '01', 45, CAST(DATEADD(day, 365, GETUTCDATE()) AS DATE), N'Acecook Việt Nam',          GETUTCDATE()),
(9,  25, '02', 20, CAST(DATEADD(day, 365, GETUTCDATE()) AS DATE), N'Acecook Việt Nam',          GETUTCDATE()),
(10, 26, '01', 5,  CAST(DATEADD(day, 730, GETUTCDATE()) AS DATE), N'Đại lý gạo Ông Cua',         GETUTCDATE()),
-- Dãy D1: bánh kẹo (Shelf ID 19-21)
(11, 19, '01', 16, CAST(DATEADD(day, 60,  GETUTCDATE()) AS DATE), N'Orion Vina',                 GETUTCDATE()),
(12, 20, '01', 12, CAST(DATEADD(day, 60,  GETUTCDATE()) AS DATE), N'Orion Vina',                 GETUTCDATE()),
(13, 21, '01', 0,  CAST(DATEADD(day, 60,  GETUTCDATE()) AS DATE), N'PepsiCo Việt Nam',          GETUTCDATE()),  -- Hết hàng!
-- Dãy D2: đậu phộng (Shelf ID 22)
(14, 22, '01', 25, CAST(DATEADD(day, 365, GETUTCDATE()) AS DATE), N'Tân Tân Food',               GETUTCDATE()),
-- Dãy C1: hoá phẩm (Shelf ID 13)
(15, 13, '01', 19, CAST(DATEADD(day, 1095,GETUTCDATE()) AS DATE), N'Unilever Việt Nam',         GETUTCDATE());
SET IDENTITY_INSERT dbo.SLOT OFF;
GO

-- 5.9. PRODUCT_SLOT (N-N - theo ERD mới)
SET IDENTITY_INSERT dbo.PRODUCT_SLOT ON;
INSERT INTO dbo.PRODUCT_SLOT (product_slot_id, slot_id, product_id) VALUES
(1,  1,  1),   -- Slot 1: Coca lon
(2,  2,  2),   -- Slot 2: Pepsi lon
(3,  3,  3),   -- Slot 3: Coca Không Đường
(4,  4,  4),   -- Slot 4: Trà xanh
(5,  5,  5),   -- Slot 5: TH True Milk
(6,  6,  6),   -- Slot 6: Vinamilk Không Đường
(7,  7,  7),   -- Slot 7: Sữa chua Vinamilk
(8,  8,  8),   -- Slot 8: Mì Hảo Hảo
(9,  9,  9),   -- Slot 9: Mì Omachi
(10, 10, 10),  -- Slot 10: Gạo ST25
(11, 11, 11),  -- Slot 11: ChocoPie
(12, 12, 12),  -- Slot 12: Custas
(13, 13, 13),  -- Slot 13: Lays
(14, 14, 14),  -- Slot 14: Đậu phộng Tân Tân
(15, 15, 15);  -- Slot 15: Sunlight
SET IDENTITY_INSERT dbo.PRODUCT_SLOT OFF;
GO

-- 5.10. Robot + Map + Navigation graph
INSERT INTO dbo.ROBOT (robot_name, robot_code, battery_pct, mode, is_online, last_seen_at) VALUES
(N'SmartBot 4WD V1', 'ROBOT-01', 88, 'idle', 1, GETUTCDATE());
GO

INSERT INTO dbo.ROBOT_ZONE (robot_id, zone_id) VALUES (1, 2), (1, 4);
GO

INSERT INTO dbo.MAP (floor_id, map_name, map_data) VALUES
(1, N'Bản đồ Tầng 1 chính thức', N'{"grid_width": 20, "grid_height": 20, "obstacle_count": 8}');
GO

-- 10 NAVIGATION_NODE (theo ERD: không có 'station' theo mới, dùng 'intersection')
SET IDENTITY_INSERT dbo.NAVIGATION_NODE ON;
INSERT INTO dbo.NAVIGATION_NODE (node_id, map_id, node_name, x_coord, y_coord, node_type, is_blocked) VALUES
(1,  1, N'Cửa ra vào siêu thị (Entrance)',          0.0, 0.0,  'entrance',     0),
(2,  1, N'Trạm sạc tự động (Home Dock)',            1.0, 0.0,  'intersection', 0),
(3,  1, N'Ngã tư trung tâm Phân khu A',             3.0, 3.0,  'intersection', 0),
(4,  1, N'Trước kệ trái cây dãy A1',                5.0, 3.0,  'shelf_front',  0),
(5,  1, N'Trước kệ rau củ dãy A2',                  7.0, 3.0,  'shelf_front',  0),
(6,  1, N'Ngã tư lối đi dãy B1',                    3.0, 7.0,  'intersection', 0),
(7,  1, N'Trước kệ nước giải khát dãy B1',          5.0, 7.0,  'shelf_front',  0),
(8,  1, N'Trước kệ sữa tươi dãy B2',                7.0, 7.0,  'shelf_front',  0),
(9,  1, N'Ngã ba hành lang hoá chất C1',            3.0, 11.0, 'intersection', 0),
(10, 1, N'Trước kệ rửa chén Sunlight dãy C1',       5.0, 11.0, 'shelf_front',  0);
SET IDENTITY_INSERT dbo.NAVIGATION_NODE OFF;
GO

INSERT INTO dbo.NAVIGATION_EDGE (from_node_id, to_node_id, distance, is_bidirectional) VALUES
(1, 2, 1.0, 1),
(1, 3, 4.2, 1),
(3, 4, 2.0, 1),
(4, 5, 2.0, 1),
(3, 6, 4.0, 1),
(6, 7, 2.0, 1),
(7, 8, 2.0, 1),
(6, 9, 4.0, 1),
(9, 10, 2.0, 1);
GO

-- 5.11. AISLE_NODE (N-N - theo ERD mới): map các aisle với node trước kệ
INSERT INTO dbo.AISLE_NODE (aisle_id, node_id) VALUES
(1, 4),   -- A1 ↔ Node 4
(2, 5),   -- A2 ↔ Node 5
(3, 7),   -- B1 ↔ Node 7
(4, 8),   -- B2 ↔ Node 8
(5, 10);  -- C1 ↔ Node 10
GO

-- 5.12. Lịch sử mua sắm (theo ERD: INVOICE_HISTORY + INVOICE_HISTORY_ITEM)
INSERT INTO dbo.INVOICE_HISTORY (member_id, purchase_date, total_amount) VALUES
(1, DATEADD(day, -1, GETUTCDATE()), 76000.00),
(2, DATEADD(day, -5, GETUTCDATE()), 90000.00);
GO

SET IDENTITY_INSERT dbo.INVOICE_HISTORY_ITEM ON;
INSERT INTO dbo.INVOICE_HISTORY_ITEM (invoice_history_item_id, invoice_history_id, product_id, quantity, unit_price) VALUES
(1, 1, 3,  2, 10500.00),  -- Huy: 2 Coca Không Đường
(2, 1, 11, 1, 55000.00),  -- Huy: 1 ChocoPie
(3, 2, 8,  20, 4500.00);  -- Hùng: 20 gói Hảo Hảo
SET IDENTITY_INSERT dbo.INVOICE_HISTORY_ITEM OFF;
GO

-- 5.13. MEAL_SUGGESTION + MEAL_ITEM
INSERT INTO dbo.MEAL_SUGGESTION (meal_name, description, yield_portions, image_url, calories, healthy_score, alternative_suggestion) VALUES
(N'Món mì trộn chua cay đặc biệt', N'Công thức chế biến đĩa mì xốt trộn giòn bùi phối trộn xúc xích rau xanh cực nhanh', 2, N'/images/recipes/mi_tron.jpg', 520, 45, N'Thay mì tôm bằng mì khoai tây Omachi để giảm dầu mỡ');
GO

INSERT INTO dbo.MEAL_ITEM (meal_suggestion_id, product_id, quantity_required, unit_of_measure) VALUES
(1, 8,  2.00, N'gói'),
(1, 14, 0.50, N'gói');
GO

-- 5.14. Brand + AdPackage + AdCampaign + SponsoredProduct + AdCampaignLog
SET IDENTITY_INSERT dbo.BRAND ON;
INSERT INTO dbo.BRAND (brand_id, brand_name, description) VALUES
(1, N'Orion Vina',   N'Tập đoàn bánh kẹo Orion Vina Hàn Quốc'),
(2, N'TH True Milk', N'Công ty cổ phần thực phẩm sữa TH'),
(3, N'Vinamilk',     N'Công ty cổ phần sữa Việt Nam'),
(4, N'Acecook',      N'Công ty TNHH Acecook Việt Nam');
SET IDENTITY_INSERT dbo.BRAND OFF;
GO

SET IDENTITY_INSERT dbo.AD_PACKAGE ON;
INSERT INTO dbo.AD_PACKAGE (package_id, package_name, price, ad_score, is_weekend_only) VALUES
(1, N'Gói Sáng Sớm (07:00-12:00)', 500000.00,  85, 0),
(2, N'Gói Cả Ngày (All Day)',      800000.00,  70, 0),
(3, N'Gói Cuối Tuần',             1200000.00, 100, 1),
(4, N'Gói Giờ Vàng (17:00-21:00)', 700000.00,  90, 0);
SET IDENTITY_INSERT dbo.AD_PACKAGE OFF;
GO

SET IDENTITY_INSERT dbo.AD_CAMPAIGN ON;
INSERT INTO dbo.AD_CAMPAIGN (ad_campaign_id, package_id, brand_id, campaign_name, start_date, end_date, is_active) VALUES
(1, 1, 1, N'Quảng cáo Bánh Orion sáng sớm tháng 6', GETUTCDATE(), DATEADD(month, 1, GETUTCDATE()), 1);
SET IDENTITY_INSERT dbo.AD_CAMPAIGN OFF;
GO

INSERT INTO dbo.SPONSORED_PRODUCT (ad_campaign_id, product_id, brand_id, priority, is_active) VALUES
(1, 12, 1, 5, 1);  -- Bánh Custas (Orion) - Campaign 1
GO

INSERT INTO dbo.AD_CAMPAIGN_LOG (ad_campaign_id, action_type) VALUES
(1, 'Created'),
(1, 'Impression'),
(1, 'Click');
GO

-- 5.15. AISLE_SCAN (lịch sử quét kệ - dùng computed needs_restock)
INSERT INTO dbo.AISLE_SCAN (aisle_id, robot_id, image_url, empty_percentage, ai_response_raw) VALUES
(3, 1, N'/storage/scans/aisle_B1_20260616.jpg', 45.50, N'{"detected_products": ["Coca","Pepsi"], "missing": 8, "confidence": 0.92}'),
(7, 1, N'/storage/scans/aisle_D1_20260616.jpg', 18.00, N'{"detected_products": ["ChocoPie","Custas"], "missing": 2, "confidence": 0.88}');
GO

-- 5.16. SEMANTIC_OBJECT (vật thể tĩnh trên bản đồ)
INSERT INTO dbo.SEMANTIC_OBJECT (map_id, object_type, x_min, y_min, x_max, y_max, label, confidence, image_url) VALUES
(1, 'shelf', 4.5,  2.5, 5.5,  3.5,  N'Kệ A1 - Trái cây',  0.95, N'/storage/objects/aisle_A1.png'),
(1, 'shelf', 6.5,  2.5, 7.5,  3.5,  N'Kệ A2 - Rau củ',    0.93, N'/storage/objects/aisle_A2.png'),
(1, 'wall',  0.0,  20.0, 20.0, 20.5, N'Tường phía Bắc',    1.00, NULL);
GO

-- 5.17. ROBOT_ROUTE + ROUTE_NODE_MAPPING + ROUTE_ASSIGNMENT (mẫu)
INSERT INTO dbo.ROBOT_ROUTE (robot_id, map_id, route_name) VALUES
(1, 1, N'Lộ trình tuần tra Khu B (nước giải khát)');
GO

-- Lộ trình: Entrance → Ngã tư A → Ngã tư B1 → Trước kệ B1 → Trước kệ B2
INSERT INTO dbo.ROUTE_NODE_MAPPING (robot_route_id, node_id, sequence_order) VALUES
(1, 1, 1),
(1, 3, 2),
(1, 6, 3),
(1, 7, 4),
(1, 8, 5);
GO

INSERT INTO dbo.ROUTE_ASSIGNMENT (robot_id, robot_route_id, status) VALUES
(1, 1, 'Completed');
GO

PRINT N'   - Đã seed đầy đủ theo ERD V4.0:';
PRINT N'     + 4 ACCOUNT, 2 MEMBER, 2 MEMBERSHIP, 7 HEALTH_TAG';
PRINT N'     + 3 MEMBERHEALTH_PREFERENCE';
PRINT N'     + 2 CATEGORY, 5 SUBCATEGORY, 9 PRODUCT_TYPE, 15 PRODUCT';
PRINT N'     + 6 PRODUCT_HEALTHTAG';
PRINT N'     + 2 INVOICE_HISTORY, 3 INVOICE_HISTORY_ITEM';
PRINT N'     + 1 MEAL_SUGGESTION, 2 MEAL_ITEM';
PRINT N'     + 1 FLOOR, 5 ZONE, 10 AISLE, 30 SHELF, 15 SLOT, 15 PRODUCT_SLOT';
PRINT N'     + 4 BRAND, 4 AD_PACKAGE, 1 AD_CAMPAIGN, 1 SPONSORED_PRODUCT, 3 AD_CAMPAIGN_LOG';
PRINT N'     + 1 ROBOT, 2 ROBOT_ZONE, 1 ROBOT_LOG (tự sinh nếu cần), 1 MAP, 10 NAVIGATION_NODE, 9 NAVIGATION_EDGE';
PRINT N'     + 5 AISLE_NODE, 1 ROBOT_ROUTE, 5 ROUTE_NODE_MAPPING, 1 ROUTE_ASSIGNMENT';
PRINT N'     + 2 AISLE_SCAN, 3 SEMANTIC_OBJECT';
GO

-- ============================================================================
-- PHẦN 6: KIỂM TRA SAU KHI CHẠY (sanity check)
-- ============================================================================
PRINT N'🔍 PHẦN 6: Kiểm tra kết quả...';
GO

DECLARE @TablesCount INT = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE');
DECLARE @ViewsCount  INT = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'VIEW');
DECLARE @AccountCount  INT = (SELECT COUNT(*) FROM dbo.ACCOUNT);
DECLARE @MemberCount   INT = (SELECT COUNT(*) FROM dbo.MEMBER);
DECLARE @ProductCount  INT = (SELECT COUNT(*) FROM dbo.PRODUCT);
DECLARE @SlotCount     INT = (SELECT COUNT(*) FROM dbo.SLOT);
DECLARE @RobotCount    INT = (SELECT COUNT(*) FROM dbo.ROBOT);
DECLARE @NavNodeCount  INT = (SELECT COUNT(*) FROM dbo.NAVIGATION_NODE);
DECLARE @AisleNodeCnt  INT = (SELECT COUNT(*) FROM dbo.AISLE_NODE);
DECLARE @RouteCount    INT = (SELECT COUNT(*) FROM dbo.ROBOT_ROUTE);

PRINT N'   - Số bảng: '  + CAST(@TablesCount AS NVARCHAR(10)) + N' (mục tiêu: 37 theo ERD V4.0)';
PRINT N'   - Số view: '   + CAST(@ViewsCount  AS NVARCHAR(10)) + N' (mục tiêu: 4)';
PRINT N'   - ACCOUNT: '   + CAST(@AccountCount AS NVARCHAR(10))  + N' rows';
PRINT N'   - MEMBER: '    + CAST(@MemberCount  AS NVARCHAR(10))  + N' rows';
PRINT N'   - PRODUCT: '   + CAST(@ProductCount AS NVARCHAR(10))  + N' rows';
PRINT N'   - SLOT: '      + CAST(@SlotCount    AS NVARCHAR(10))  + N' rows';
PRINT N'   - ROBOT: '     + CAST(@RobotCount   AS NVARCHAR(10))  + N' rows';
PRINT N'   - NAVIGATION_NODE: ' + CAST(@NavNodeCount AS NVARCHAR(10)) + N' rows';
PRINT N'   - AISLE_NODE: ' + CAST(@AisleNodeCnt AS NVARCHAR(10)) + N' rows';
PRINT N'   - ROBOT_ROUTE: ' + CAST(@RouteCount   AS NVARCHAR(10)) + N' rows';
GO

-- Test view
SELECT TOP 1 * FROM dbo.PurchaseHistory;
SELECT TOP 1 * FROM dbo.Store_Map;
SELECT TOP 1 * FROM dbo.Blocked_Aisles;
SELECT TOP 1 * FROM dbo.Real_Time_Stock;
GO

-- ============================================================================
-- PHẦN 7: HOÀN TẤT
-- ============================================================================
PRINT N'====================================================================';
PRINT N'  ✅ SUCCESS: DATABASE [SuperMarketBot] INITIALIZED IN ONE-SHOT!';
PRINT N'  ✅ 37 Tables + 4 Views + Full Vietnamese Supermarket Seed Data';
PRINT N'  ✅ Style: SNAKE_CASE (UPPER_CASE table, snake_case columns)';
PRINT N'  ✅ Tương thích: AI FastAPI + n8n (qua 4 view) + IoT MQTT (qua ROBOT_LOG)';
PRINT N'';
PRINT N'  📌 BƯỚC TIẾP THEO:';
PRINT N'  1. Cập nhật connection string trong appsettings.json:';
PRINT N'     "DefaultConnection": "Server=localhost;Database=SuperMarketBot;';
PRINT N'                          Trusted_Connection=True;TrustServerCertificate=True;"';
PRINT N'  2. (Backend .NET) Cập nhật Entity classes + DbContext mapping theo';
PRINT N'     snake_case schema mới - chạy scaffold nếu cần:';
PRINT N'     dotnet ef dbcontext scaffold "connection-string" \\';
PRINT N'         Microsoft.EntityFrameworkCore.SqlServer -o Domain/Entities';
PRINT N'  3. dotnet run --project src/SmartMarketBot.API';
PRINT N'  4. Mở http://localhost:5000/swagger để test';
PRINT N'';
PRINT N'  👤 Tài khoản demo:';
PRINT N'     admin:   admin_lth   / hash_pbkdf2_code_123';
PRINT N'     member:  member_qhuy / hash_pbkdf2_code_123 (Huy - Gold)';
PRINT N'     member:  member_ahung/ hash_pbkdf2_code_123 (Hùng - Allergy)';
PRINT N'     staff:   staff_dtnhan/ hash_pbkdf2_code_123';
PRINT N'';
PRINT N'  ⚠️  LƯU Ý QUAN TRỌNG:';
PRINT N'  - ERD V4.0 ĐÃ ĐỔI TÊN BẢNG (snake_case) so với code hiện tại.';
PRINT N'  - CẦN cập nhật lại Entity classes + DbContext mapping trong code';
PRINT N'    trước khi chạy backend (chưa thực hiện theo yêu cầu sql_only).';
PRINT N'====================================================================';
GO
