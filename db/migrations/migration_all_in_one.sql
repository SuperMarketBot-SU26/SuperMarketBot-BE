-- ============================================================================
-- SmartMarketBot — Migration All-In-One
-- Phase C: 3 Luồng Targeting (Route + Zone + Shelf) — Idempotent
--
-- File này gộp TẤT CẢ thay đổi cần thiết để đồng bộ DB với code:
--   1. Tạo bảng AD_CAMPAIGN_ROUTE (N-N Campaign ↔ RobotRoute + snapshot)
--   2. Tạo bảng AD_CAMPAIGN_ZONE  (N-N Campaign ↔ Zone      + snapshot)
--   3. Đổi AD_CAMPAIGN.RobotZoneID → SemanticObjectID
--   4. AD_CAMPAIGN: thêm ShelfPriceCharged + ShelfPurchasedAt
--   5. AD_CAMPAIGN_LOG: thêm SemanticObjectID
--   6. AD_PACKAGE: thêm PriceZone + PriceShelf (tách Price thành 3 cột)
--   7. Backfill giá demo cho 3 package
--
-- Đặc điểm:
--   • Chạy 1 lần duy nhất, không cần thứ tự — tất cả IF NOT EXISTS.
--   • Có thể chạy nhiều lần không lỗi (idempotent).
--   • Dùng cho cả DB mới (sau khi chạy erd_database.sql) và DB cũ đang có data.
--
-- Cách dùng:
--   sqlcmd -S <server> -d SuperMarketBot -i db/migrations/migration_all_in_one.sql
-- ============================================================================

USE SuperMarketBot;
GO

PRINT '=== SmartMarketBot Migration All-In-One START ===';
GO

-- ════════════════════════════════════════════════════════════════════════
-- 1) AD_CAMPAIGN_ROUTE — Bảng mới: N-N Campaign ↔ RobotRoute
--    Snap giá tại lúc mua route. Charge mỗi impression bằng RoutePriceCharged.
-- ════════════════════════════════════════════════════════════════════════
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AD_CAMPAIGN_ROUTE')
BEGIN
    CREATE TABLE dbo.AD_CAMPAIGN_ROUTE (
        AdCampaignID        INT            NOT NULL,
        RobotRouteID        INT            NOT NULL,
        RoutePriceCharged   DECIMAL(18,2)  NOT NULL DEFAULT 0,
        PurchasedAt         DATETIME2      NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE()),
        CONSTRAINT PK_AD_CAMPAIGN_ROUTE PRIMARY KEY (AdCampaignID, RobotRouteID),
        CONSTRAINT FK_ACR_ADCAMPAIGN FOREIGN KEY (AdCampaignID) REFERENCES dbo.AD_CAMPAIGN(AdCampaignID) ON DELETE CASCADE,
        CONSTRAINT FK_ACR_ROBOT_ROUTE FOREIGN KEY (RobotRouteID) REFERENCES dbo.ROBOT_ROUTE(RobotRouteID) ON DELETE CASCADE
    );
    PRINT '[1/7] Created table AD_CAMPAIGN_ROUTE';
END
ELSE
    PRINT '[1/7] AD_CAMPAIGN_ROUTE already exists — skipped';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.AD_CAMPAIGN_ROUTE') AND name = 'IX_ACR_RobotRouteID')
BEGIN
    CREATE INDEX IX_ACR_RobotRouteID ON dbo.AD_CAMPAIGN_ROUTE(RobotRouteID);
    PRINT '[1/7] Created index IX_ACR_RobotRouteID';
END
GO

-- ════════════════════════════════════════════════════════════════════════
-- 2) AD_CAMPAIGN_ZONE — Bảng mới: N-N Campaign ↔ Zone
--    Snap giá tại lúc mua zone. Charge mỗi impression bằng ZonePriceCharged.
-- ════════════════════════════════════════════════════════════════════════
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AD_CAMPAIGN_ZONE')
BEGIN
    CREATE TABLE dbo.AD_CAMPAIGN_ZONE (
        AdCampaignID      INT            NOT NULL,
        ZoneID            INT            NOT NULL,
        CONSTRAINT PK_AD_CAMPAIGN_ZONE PRIMARY KEY (AdCampaignID, ZoneID),
        CONSTRAINT FK_ACZ_ADCAMPAIGN FOREIGN KEY (AdCampaignID) REFERENCES dbo.AD_CAMPAIGN(AdCampaignID) ON DELETE CASCADE,
        CONSTRAINT FK_ACZ_ZONE        FOREIGN KEY (ZoneID)       REFERENCES dbo.ZONE(ZoneID)             ON DELETE CASCADE
    );
    PRINT '[2/7] Created table AD_CAMPAIGN_ZONE';
END
ELSE
    PRINT '[2/7] AD_CAMPAIGN_ZONE already exists — skipped';
GO

-- Thêm 2 cột snapshot giá zone (idempotent)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.AD_CAMPAIGN_ZONE') AND name = 'ZonePriceCharged')
BEGIN
    ALTER TABLE dbo.AD_CAMPAIGN_ZONE ADD ZonePriceCharged DECIMAL(18,2) NOT NULL DEFAULT 0;
    PRINT '[2/7] Added column AD_CAMPAIGN_ZONE.ZonePriceCharged';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.AD_CAMPAIGN_ZONE') AND name = 'PurchasedAt')
BEGIN
    ALTER TABLE dbo.AD_CAMPAIGN_ZONE ADD PurchasedAt DATETIME2 NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE());
    PRINT '[2/7] Added column AD_CAMPAIGN_ZONE.PurchasedAt';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.AD_CAMPAIGN_ZONE') AND name = 'IX_ACZ_ZoneID')
BEGIN
    CREATE INDEX IX_ACZ_ZoneID ON dbo.AD_CAMPAIGN_ZONE(ZoneID);
    PRINT '[2/7] Created index IX_ACZ_ZoneID';
END
GO

-- ════════════════════════════════════════════════════════════════════════
-- 3) AD_CAMPAIGN — Đổi RobotZoneID → SemanticObjectID + thêm 2 cột snapshot shelf
-- ════════════════════════════════════════════════════════════════════════
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.AD_CAMPAIGN') AND name = 'SemanticObjectID'
)
BEGIN
    ALTER TABLE dbo.AD_CAMPAIGN ADD SemanticObjectID INT NULL;
    PRINT '[3/7] Added column AD_CAMPAIGN.SemanticObjectID';

    IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SEMANTIC_OBJECT')
       AND NOT EXISTS (
           SELECT 1 FROM sys.foreign_keys
           WHERE name = 'FK_AD_CAMPAIGN_SEMANTIC_OBJECT'
       )
    BEGIN
        ALTER TABLE dbo.AD_CAMPAIGN
            ADD CONSTRAINT FK_AD_CAMPAIGN_SEMANTIC_OBJECT
            FOREIGN KEY (SemanticObjectID) REFERENCES dbo.SEMANTIC_OBJECT(ObjectID) ON DELETE SET NULL;
        PRINT '[3/7] Added FK FK_AD_CAMPAIGN_SEMANTIC_OBJECT';
    END
END
ELSE
    PRINT '[3/7] AD_CAMPAIGN.SemanticObjectID already exists — skipped';
GO

-- Snapshot giá shelf (cho Phase C)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.AD_CAMPAIGN') AND name = 'ShelfPriceCharged'
)
BEGIN
    ALTER TABLE dbo.AD_CAMPAIGN ADD ShelfPriceCharged DECIMAL(18,2) NOT NULL DEFAULT 0;
    PRINT '[3/7] Added column AD_CAMPAIGN.ShelfPriceCharged';
END

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.AD_CAMPAIGN') AND name = 'ShelfPurchasedAt'
)
BEGIN
    ALTER TABLE dbo.AD_CAMPAIGN ADD ShelfPurchasedAt DATETIME2 NULL;
    PRINT '[3/7] Added column AD_CAMPAIGN.ShelfPurchasedAt';
END
GO

-- (Optional) Drop cột RobotZoneID cũ — KHÔNG drop tự động để an toàn.
-- Nếu muốn xóa, chạy thủ công:
--   ALTER TABLE dbo.AD_CAMPAIGN DROP CONSTRAINT FK_AD_CAMPAIGN_ROBOT_ZONE;  -- nếu có
--   ALTER TABLE dbo.AD_CAMPAIGN DROP COLUMN RobotZoneID;

-- ════════════════════════════════════════════════════════════════════════
-- 4) AD_CAMPAIGN_LOG — Thêm SemanticObjectID (cho RoutePass tracking)
-- ════════════════════════════════════════════════════════════════════════
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.AD_CAMPAIGN_LOG') AND name = 'SemanticObjectID'
)
BEGIN
    ALTER TABLE dbo.AD_CAMPAIGN_LOG ADD SemanticObjectID INT NULL;
    PRINT '[4/7] Added column AD_CAMPAIGN_LOG.SemanticObjectID';

    IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SEMANTIC_OBJECT')
       AND NOT EXISTS (
           SELECT 1 FROM sys.foreign_keys
           WHERE parent_object_id = OBJECT_ID('dbo.AD_CAMPAIGN_LOG')
             AND name = 'FK_AD_LOG_SEMANTIC_OBJECT'
       )
    BEGIN
        ALTER TABLE dbo.AD_CAMPAIGN_LOG
            ADD CONSTRAINT FK_AD_LOG_SEMANTIC_OBJECT
            FOREIGN KEY (SemanticObjectID) REFERENCES dbo.SEMANTIC_OBJECT(ObjectID) ON DELETE SET NULL;
        PRINT '[4/7] Added FK FK_AD_LOG_SEMANTIC_OBJECT';
    END
END
ELSE
    PRINT '[4/7] AD_CAMPAIGN_LOG.SemanticObjectID already exists — skipped';
GO

-- ════════════════════════════════════════════════════════════════════════
-- 5) AD_PACKAGE — Tách Price thành 3 cột: PriceRoute + PriceZone + PriceShelf
--    Ý nghĩa mới: mỗi cột là đơn giá / 1 đơn vị (route/zone/shelf).
-- ════════════════════════════════════════════════════════════════════════
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.AD_PACKAGE') AND name = 'PriceZone'
)
BEGIN
    ALTER TABLE dbo.AD_PACKAGE ADD PriceZone DECIMAL(18,2) NOT NULL DEFAULT 0;
    PRINT '[5/7] Added column AD_PACKAGE.PriceZone';
END

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.AD_PACKAGE') AND name = 'PriceShelf'
)
BEGIN
    ALTER TABLE dbo.AD_PACKAGE ADD PriceShelf DECIMAL(18,2) NOT NULL DEFAULT 0;
    PRINT '[5/7] Added column AD_PACKAGE.PriceShelf';
END
GO

-- ════════════════════════════════════════════════════════════════════════
-- 6) Backfill giá mặc định cho 3 package demo (idempotent — luôn UPDATE)
--    Chỉ chạy nếu giá hiện tại = 0 (nghĩa là chưa từng set).
-- ════════════════════════════════════════════════════════════════════════
UPDATE dbo.AD_PACKAGE SET PriceZone = 30000, PriceShelf = 15000 WHERE PackageID = 1 AND PriceZone = 0;
UPDATE dbo.AD_PACKAGE SET PriceZone = 50000, PriceShelf = 25000 WHERE PackageID = 2 AND PriceZone = 0;
UPDATE dbo.AD_PACKAGE SET PriceZone = 80000, PriceShelf = 40000 WHERE PackageID = 3 AND PriceZone = 0;
PRINT '[6/7] Backfilled demo prices for AD_PACKAGE (where PriceZone = 0)';
GO

-- ════════════════════════════════════════════════════════════════════════
-- 7) (Optional) Backfill route/zone/shelf snapshot cho campaign demo cũ
--    Chỉ áp dụng khi campaign đang dùng giá mặc định (= 0).
--    Không phá vỡ data đã snapshot đúng.
-- ════════════════════════════════════════════════════════════════════════
-- RoutePriceCharged: nếu chưa có dữ liệu AD_CAMPAIGN_ROUTE, set snapshot = PriceRoute của package
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AD_CAMPAIGN_ROUTE')
   AND NOT EXISTS (SELECT 1 FROM dbo.AD_CAMPAIGN_ROUTE)
   AND EXISTS (SELECT 1 FROM dbo.AD_PACKAGE WHERE PriceRoute > 0)
BEGIN
    -- Insert 1 route mặc định cho mỗi Active campaign để test
    INSERT INTO dbo.AD_CAMPAIGN_ROUTE (AdCampaignID, RobotRouteID, RoutePriceCharged, PurchasedAt)
    SELECT ac.AdCampaignID,
           1,
           pkg.PriceRoute,
           DATEADD(hour, 7, GETUTCDATE())
    FROM dbo.AD_CAMPAIGN ac
    JOIN dbo.AD_PACKAGE pkg ON ac.PackageID = pkg.PackageID
    WHERE ac.Status = 'Active' AND pkg.PriceRoute > 0;
    PRINT '[7/7] Backfilled AD_CAMPAIGN_ROUTE for Active campaigns';
END
ELSE
    PRINT '[7/7] AD_CAMPAIGN_ROUTE backfill skipped (already has data)';
GO

-- ZonePriceCharged: tương tự — chỉ chạy nếu bảng AD_CAMPAIGN_ZONE trống
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AD_CAMPAIGN_ZONE')
   AND NOT EXISTS (SELECT 1 FROM dbo.AD_CAMPAIGN_ZONE)
   AND EXISTS (SELECT 1 FROM dbo.AD_PACKAGE WHERE PriceZone > 0)
BEGIN
    INSERT INTO dbo.AD_CAMPAIGN_ZONE (AdCampaignID, ZoneID, ZonePriceCharged, PurchasedAt)
    SELECT ac.AdCampaignID,
           1,
           pkg.PriceZone,
           DATEADD(hour, 7, GETUTCDATE())
    FROM dbo.AD_CAMPAIGN ac
    JOIN dbo.AD_PACKAGE pkg ON ac.PackageID = pkg.PackageID
    WHERE ac.Status = 'Active' AND pkg.PriceZone > 0;
    PRINT '[7/7] Backfilled AD_CAMPAIGN_ZONE for Active campaigns';
END
ELSE
    PRINT '[7/7] AD_CAMPAIGN_ZONE backfill skipped (already has data)';
GO

PRINT '=== SmartMarketBot Migration All-In-One DONE ===';
GO

-- ════════════════════════════════════════════════════════════════════════
-- VERIFY (Optional — bỏ comment nếu muốn xem ngay schema sau migration)
-- ════════════════════════════════════════════════════════════════════════
/*
SELECT 'AD_PACKAGE' AS [Table], COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'AD_PACKAGE'
  AND COLUMN_NAME IN ('PricePackage', 'PriceRoute', 'PriceZone', 'PriceShelf');

SELECT 'AD_CAMPAIGN' AS [Table], COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'AD_CAMPAIGN'
  AND COLUMN_NAME IN ('SemanticObjectID', 'ShelfPriceCharged', 'ShelfPurchasedAt');

SELECT 'AD_CAMPAIGN_LOG' AS [Table], COLUMN_NAME, DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'AD_CAMPAIGN_LOG' AND COLUMN_NAME = 'SemanticObjectID';

SELECT 'AD_CAMPAIGN_ROUTE' AS [Table], COUNT(*) AS RowCount FROM dbo.AD_CAMPAIGN_ROUTE;
SELECT 'AD_CAMPAIGN_ZONE'  AS [Table], COUNT(*) AS RowCount FROM dbo.AD_CAMPAIGN_ZONE;
*/