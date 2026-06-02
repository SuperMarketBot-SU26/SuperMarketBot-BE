/*
  V3.1 — Capstone Full Schema (idempotent, safe to re-run)
  Áp dụng trên DB SuperMarketBot đã có sẵn (sau V3.0).
  Thêm: 14 cột mới trên 5 bảng + 3 bảng mới (36, 37, 38) + Seed data.
  Tham chiếu: Buổi 10, 13, 14, 15, 16, 17 — Thầy Đỗ Tấn Nhàn.
*/
USE SuperMarketBot;
GO
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ══════════════════════════════════════════════════════
-- 1. Members — SearchMode + ShoppingBudget (Buổi 16)
-- ══════════════════════════════════════════════════════
IF COL_LENGTH('dbo.Members', 'SearchMode') IS NULL
    ALTER TABLE dbo.Members ADD SearchMode NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Members_SearchMode DEFAULT 'Normal';
GO
IF COL_LENGTH('dbo.Members', 'ShoppingBudget') IS NULL
    ALTER TABLE dbo.Members ADD ShoppingBudget DECIMAL(12,2) NULL;
GO

-- ══════════════════════════════════════════════════════
-- 2. Slots — ExpiryDate + Supplier (Buổi 13)
-- ══════════════════════════════════════════════════════
IF COL_LENGTH('dbo.Slots', 'ExpiryDate') IS NULL
    ALTER TABLE dbo.Slots ADD ExpiryDate DATE NULL;
GO
IF COL_LENGTH('dbo.Slots', 'Supplier') IS NULL
    ALTER TABLE dbo.Slots ADD Supplier NVARCHAR(200) NULL;
GO

-- ══════════════════════════════════════════════════════
-- 3. Recipes — Calories + HealthyScore + AlternativeSuggestion (Buổi 13)
-- ══════════════════════════════════════════════════════
IF COL_LENGTH('dbo.Recipes', 'Calories') IS NULL
    ALTER TABLE dbo.Recipes ADD Calories INT NULL;
GO
IF COL_LENGTH('dbo.Recipes', 'HealthyScore') IS NULL
    ALTER TABLE dbo.Recipes ADD HealthyScore INT NULL;
GO
IF COL_LENGTH('dbo.Recipes', 'AlternativeSuggestion') IS NULL
    ALTER TABLE dbo.Recipes ADD AlternativeSuggestion NVARCHAR(500) NULL;
GO

-- ══════════════════════════════════════════════════════
-- 4. SponsoredProducts — Ad bidding fields (Buổi 14, 16, 17)
-- ══════════════════════════════════════════════════════
IF COL_LENGTH('dbo.SponsoredProducts', 'AdScore') IS NULL
    ALTER TABLE dbo.SponsoredProducts ADD AdScore INT NOT NULL
        CONSTRAINT DF_SponsoredProducts_AdScore DEFAULT 0;
GO
IF COL_LENGTH('dbo.SponsoredProducts', 'TimeSlotStart') IS NULL
    ALTER TABLE dbo.SponsoredProducts ADD TimeSlotStart TIME NULL;
GO
IF COL_LENGTH('dbo.SponsoredProducts', 'TimeSlotEnd') IS NULL
    ALTER TABLE dbo.SponsoredProducts ADD TimeSlotEnd TIME NULL;
GO
IF COL_LENGTH('dbo.SponsoredProducts', 'IsWeekendOnly') IS NULL
    ALTER TABLE dbo.SponsoredProducts ADD IsWeekendOnly BIT NOT NULL
        CONSTRAINT DF_SponsoredProducts_IsWeekendOnly DEFAULT 0;
GO
IF COL_LENGTH('dbo.SponsoredProducts', 'BidPrice') IS NULL
    ALTER TABLE dbo.SponsoredProducts ADD BidPrice DECIMAL(12,2) NOT NULL
        CONSTRAINT DF_SponsoredProducts_BidPrice DEFAULT 0.00;
GO
IF COL_LENGTH('dbo.SponsoredProducts', 'WeekendMultiplier') IS NULL
    ALTER TABLE dbo.SponsoredProducts ADD WeekendMultiplier DECIMAL(3,2) NOT NULL
        CONSTRAINT DF_SponsoredProducts_WeekendMultiplier DEFAULT 1.00;
GO

-- ══════════════════════════════════════════════════════
-- 5. ShelfScans — IsOccluded + OcclusionReason (Buổi 15)
-- ══════════════════════════════════════════════════════
IF COL_LENGTH('dbo.ShelfScans', 'IsOccluded') IS NULL
    ALTER TABLE dbo.ShelfScans ADD IsOccluded BIT NOT NULL
        CONSTRAINT DF_ShelfScans_IsOccluded DEFAULT 0;
GO
IF COL_LENGTH('dbo.ShelfScans', 'OcclusionReason') IS NULL
    ALTER TABLE dbo.ShelfScans ADD OcclusionReason NVARCHAR(255) NULL;
GO

-- ══════════════════════════════════════════════════════
-- 6. Bảng 36: ForbiddenZones (Buổi 10)
-- ══════════════════════════════════════════════════════
IF OBJECT_ID('dbo.ForbiddenZones', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ForbiddenZones (
        ForbiddenZoneID INT IDENTITY(1,1) NOT NULL,
        MapID           INT NOT NULL,
        ZoneName        NVARCHAR(100) NOT NULL,
        XMin            FLOAT NOT NULL,
        YMin            FLOAT NOT NULL,
        XMax            FLOAT NOT NULL,
        YMax            FLOAT NOT NULL,
        IsActive        BIT NOT NULL CONSTRAINT DF_ForbiddenZones_IsActive DEFAULT 1,
        Reason          NVARCHAR(255) NULL,
        CONSTRAINT PK_ForbiddenZones PRIMARY KEY (ForbiddenZoneID),
        CONSTRAINT FK_ForbiddenZones_Maps FOREIGN KEY (MapID)
            REFERENCES dbo.Maps(MapID) ON DELETE CASCADE
    );
    PRINT 'Created: dbo.ForbiddenZones';
END
GO

-- ══════════════════════════════════════════════════════
-- 7. Bảng 37: MemberAlerts (Buổi 16)
-- ══════════════════════════════════════════════════════
IF OBJECT_ID('dbo.MemberAlerts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MemberAlerts (
        AlertID      INT IDENTITY(1,1) NOT NULL,
        MemberID     INT NOT NULL,
        AlertType    NVARCHAR(50) NOT NULL,  -- 'Allergy','BudgetExceeded','DuplicatePurchase','OutOfStock'
        AlertMessage NVARCHAR(500) NOT NULL,
        CreatedAt    DATETIME2 NOT NULL CONSTRAINT DF_MemberAlerts_CreatedAt DEFAULT GETUTCDATE(),
        IsRead       BIT NOT NULL CONSTRAINT DF_MemberAlerts_IsRead DEFAULT 0,
        CONSTRAINT PK_MemberAlerts PRIMARY KEY (AlertID),
        CONSTRAINT FK_MemberAlerts_Members FOREIGN KEY (MemberID)
            REFERENCES dbo.Members(MemberID) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX IX_MemberAlerts_MemberID_IsRead ON dbo.MemberAlerts(MemberID, IsRead);
    PRINT 'Created: dbo.MemberAlerts';
END
GO

-- ══════════════════════════════════════════════════════
-- 8. Bảng 38: MemberEvents (Buổi 16)
-- ══════════════════════════════════════════════════════
IF OBJECT_ID('dbo.MemberEvents', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MemberEvents (
        EventID     INT IDENTITY(1,1) NOT NULL,
        MemberID    INT NOT NULL,
        EventName   NVARCHAR(100) NOT NULL,  -- 'Birthday','Anniversary','VIPUpgrade'
        EventDate   DATE NOT NULL,
        DiscountPct DECIMAL(5,2) NULL,
        IsProcessed BIT NOT NULL CONSTRAINT DF_MemberEvents_IsProcessed DEFAULT 0,
        CONSTRAINT PK_MemberEvents PRIMARY KEY (EventID),
        CONSTRAINT FK_MemberEvents_Members FOREIGN KEY (MemberID)
            REFERENCES dbo.Members(MemberID) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX IX_MemberEvents_EventDate ON dbo.MemberEvents(EventDate, IsProcessed);
    PRINT 'Created: dbo.MemberEvents';
END
GO

-- ══════════════════════════════════════════════════════
-- 9. Seed Data cho các bảng & cột mới
-- ══════════════════════════════════════════════════════

-- ForbiddenZones (chỉ insert nếu chưa có)
IF NOT EXISTS (SELECT 1 FROM dbo.ForbiddenZones WHERE ZoneName = N'Khu vực quầy thu ngân')
    INSERT INTO dbo.ForbiddenZones (MapID, ZoneName, XMin, YMin, XMax, YMax, IsActive, Reason) VALUES 
    (1, N'Khu vực quầy thu ngân', 8.0, 0.0, 12.0, 3.0, 1, N'Robot không được đi gần quầy thu ngân khi có khách'),
    (1, N'Hành lang thoát hiểm', 0.0, 8.0, 2.0, 20.0, 1, N'Lối thoát hiểm bắt buộc luôn thông thoáng');
GO

-- MemberAlerts
IF NOT EXISTS (SELECT 1 FROM dbo.MemberAlerts WHERE MemberID = 2 AND AlertType = 'Allergy')
    INSERT INTO dbo.MemberAlerts (MemberID, AlertType, AlertMessage, IsRead) VALUES 
    (2, 'Allergy',         N'⚠️ CẢNH BÁO DỊ ỨNG: Đậu phộng Tân Tân (8935049500145) chứa hạt lạc — Hùng dị ứng hạt lạc!', 0),
    (1, 'BudgetExceeded',  N'💰 Giỏ hàng 210.000₫ vượt ngân sách 200.000₫ đã cài đặt.', 0);
GO

-- MemberEvents
IF NOT EXISTS (SELECT 1 FROM dbo.MemberEvents WHERE MemberID = 1 AND EventName = 'Birthday')
    INSERT INTO dbo.MemberEvents (MemberID, EventName, EventDate, DiscountPct, IsProcessed) VALUES 
    (1, 'Birthday',    CAST(DATEADD(day, 3, GETUTCDATE()) AS DATE), 15.00, 0),
    (2, 'Anniversary', CAST(DATEADD(day, 7, GETUTCDATE()) AS DATE), 10.00, 0);
GO

-- Cập nhật Members SearchMode + ShoppingBudget
UPDATE dbo.Members SET SearchMode = 'Healthy', ShoppingBudget = 200000.00 WHERE MemberID = 1 AND SearchMode = 'Normal';
UPDATE dbo.Members SET SearchMode = 'Budget',  ShoppingBudget = 150000.00 WHERE MemberID = 2 AND SearchMode = 'Normal';
GO

-- Cập nhật Recipes Calories + HealthyScore
UPDATE dbo.Recipes SET Calories = 520, HealthyScore = 45, AlternativeSuggestion = N'Thay mì tôm bằng mì khoai tây Omachi để giảm dầu mỡ' WHERE RecipeID = 1;
GO

-- Cập nhật SponsoredProducts AdScore + BidPrice + khung giờ + multiplier
UPDATE dbo.SponsoredProducts SET AdScore = 85, TimeSlotStart = '07:00:00', TimeSlotEnd = '12:00:00', BidPrice = 500.00, WeekendMultiplier = 1.50 WHERE SponsoredID = 1;
GO

-- Cập nhật Slots ExpiryDate + Supplier cho sản phẩm mẫu
UPDATE dbo.Slots SET ExpiryDate = CAST(DATEADD(day, 30, GETUTCDATE()) AS DATE),  Supplier = N'Vinamilk' WHERE ProductID IN (5, 6, 7);
UPDATE dbo.Slots SET ExpiryDate = CAST(DATEADD(day, 180, GETUTCDATE()) AS DATE), Supplier = N'Acecook Việt Nam' WHERE ProductID IN (8, 9);
UPDATE dbo.Slots SET ExpiryDate = CAST(DATEADD(day, 365, GETUTCDATE()) AS DATE), Supplier = N'Tân Tân Food' WHERE ProductID = 14;
GO

-- ══════════════════════════════════════════════════════
-- 10. Ghi nhận migration history
-- ══════════════════════════════════════════════════════
IF OBJECT_ID('dbo.__EFMigrationsHistory', 'U') IS NULL
    CREATE TABLE dbo.[__EFMigrationsHistory] (
        MigrationId    NVARCHAR(150) NOT NULL,
        ProductVersion NVARCHAR(32)  NOT NULL,
        CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY (MigrationId)
    );
GO
IF NOT EXISTS (SELECT 1 FROM dbo.[__EFMigrationsHistory] WHERE MigrationId = N'20260602_CapstoneFullSchema')
    INSERT INTO dbo.[__EFMigrationsHistory] VALUES (N'20260602_CapstoneFullSchema', N'10.0.0');
GO

-- ══════════════════════════════════════════════════════
-- 11. Verify
-- ══════════════════════════════════════════════════════
SELECT COUNT(*) AS TotalTables FROM sys.tables;

SELECT t.name AS [Table], c.name AS [NewColumn]
FROM sys.columns c
JOIN sys.tables t ON c.object_id = t.object_id
WHERE t.name IN ('Members','Slots','Recipes','SponsoredProducts','ShelfScans')
  AND c.name IN ('SearchMode','ShoppingBudget','ExpiryDate','Supplier',
                 'Calories','HealthyScore','AlternativeSuggestion',
                 'AdScore','TimeSlotStart','TimeSlotEnd','IsWeekendOnly','BidPrice','WeekendMultiplier',
                 'IsOccluded','OcclusionReason')
ORDER BY t.name, c.name;

PRINT '====================================================================';
PRINT '  V3.1 — Capstone Full Schema applied successfully!';
PRINT '  38 Tables | 14 New Columns | 3 New Tables';
PRINT '====================================================================';
