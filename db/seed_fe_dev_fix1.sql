-- ============================================================
-- Fix script: Xóa data cũ trong các bảng ROBOT/ROBOT_*/BRAND/AD_*
-- (do seed_v4 đã insert ID=1 rồi, conflict với seed_fe_dev cũng dùng ID=2)
-- Sau đó insert lại theo đúng thứ tự FK.
-- ============================================================

USE SuperMarketBot;
GO

-- Xóa các bảng phụ thuộc theo FK ngược
DELETE FROM dbo.AD_CAMPAIGN_LOG;
DELETE FROM dbo.SPONSORED_PRODUCT;
DELETE FROM dbo.AD_CAMPAIGN;
DELETE FROM dbo.AD_PACKAGE;
DELETE FROM dbo.BRAND;
DELETE FROM dbo.ROBOT_ZONE;
DELETE FROM dbo.ROBOT_LOG;
DELETE FROM dbo.ROBOT;
GO

DBCC CHECKIDENT('dbo.ROBOT', RESEED, 0);
DBCC CHECKIDENT('dbo.ROBOT_LOG', RESEED, 0);
DBCC CHECKIDENT('dbo.ROBOT_ZONE', RESEED, 0);
DBCC CHECKIDENT('dbo.BRAND', RESEED, 0);
DBCC CHECKIDENT('dbo.AD_PACKAGE', RESEED, 0);
DBCC CHECKIDENT('dbo.AD_CAMPAIGN', RESEED, 0);
DBCC CHECKIDENT('dbo.SPONSORED_PRODUCT', RESEED, 0);
DBCC CHECKIDENT('dbo.AD_CAMPAIGN_LOG', RESEED, 0);
GO

-- ══════════════════════════════════════════════════════════════
-- 17. ROBOT (5 record: ID 1-5)
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.ROBOT ON;
INSERT INTO dbo.ROBOT (RobotID, RobotName, RobotCode, BatteryPct, Mode, Status, LastSeenAt) VALUES
(1, N'Robot 01', N'RB001', 100, N'idle', N'Online', DATEADD(hour, 7, GETUTCDATE())),
(2, N'Robot 02', N'RB002', 87, N'idle', N'Online', DATEADD(minute, -2, DATEADD(hour, 7, GETUTCDATE()))),
(3, N'Robot 03', N'RB003', 45, N'navigating', N'Online', DATEADD(minute, -1, DATEADD(hour, 7, GETUTCDATE()))),
(4, N'Robot 04', N'RB004', 15, N'charging', N'Online', DATEADD(hour, -1, DATEADD(hour, 7, GETUTCDATE()))),
(5, N'Robot 05', N'RB005', 92, N'scanning', N'Online', DATEADD(minute, -5, DATEADD(hour, 7, GETUTCDATE())));
SET IDENTITY_INSERT dbo.ROBOT OFF;
GO

-- ══════════════════════════════════════════════════════════════
-- 18. ROBOT_ZONE (10 record: ID 1-10)
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.ROBOT_ZONE ON;
INSERT INTO dbo.ROBOT_ZONE (RobotZoneID, RobotID, ZoneID) VALUES
(1, 1, 1),
(2, 1, 2),
(3, 2, 3),
(4, 2, 4),
(5, 3, 5),
(6, 3, 6),
(7, 4, 3),
(8, 4, 4),
(9, 5, 3),
(10, 5, 6);
SET IDENTITY_INSERT dbo.ROBOT_ZONE OFF;
GO

-- ══════════════════════════════════════════════════════════════
-- 19. ROBOT_LOG (20 record)
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.ROBOT_LOG ON;
DECLARE @i INT = 1;
DECLARE @robotId INT;
WHILE @i <= 20
BEGIN
    SET @robotId = ((@i - 1) % 5) + 1;
    INSERT INTO dbo.ROBOT_LOG (LogID, RobotID, battery, location, status, timestamp, XCoord, YCoord, HeadingRad)
    VALUES (
        @i,
        @robotId,
        30 + ((@i * 7) % 70),
        N'Zone ' + CAST(((@i % 6) + 1) AS VARCHAR) + N', Cell ' + CAST((@i % 12) AS VARCHAR),
        CASE @i % 4
            WHEN 0 THEN N'navigating'
            WHEN 1 THEN N'scanning'
            WHEN 2 THEN N'charging'
            ELSE N'idle'
        END,
        DATEADD(minute, -(@i * 10), DATEADD(hour, 7, GETUTCDATE())),
        (CAST((@i % 100) AS FLOAT) * 1.5),
        (CAST((@i % 80) AS FLOAT) * 1.2),
        CAST(((@i * 0.314) % 6.28) AS FLOAT)
    );
    SET @i = @i + 1;
END
SET IDENTITY_INSERT dbo.ROBOT_LOG OFF;
GO

-- ══════════════════════════════════════════════════════════════
-- 20. BRAND (5 record: ID 1-5)
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.BRAND ON;
INSERT INTO dbo.BRAND (BrandID, BrandName, Wallet, Description) VALUES
(1, N'Coca-Cola Company', 1000000.00, N'Hãng nước giải khát'),
(2, N'Unilever Vietnam', 5000000.00, N'Nhà sản xuất hàng tiêu dùng'),
(3, N'Vinamilk', 8000000.00, N'Sữa và sản phẩm từ sữa'),
(4, N'Sunhouse Group', 3500000.00, N'Đồ gia dụng'),
(5, N'Panasonic Vietnam', 2500000.00, N'Đồ điện gia dụng');
SET IDENTITY_INSERT dbo.BRAND OFF;
GO

-- ══════════════════════════════════════════════════════════════
-- 21. AD_PACKAGE (3 record: ID 1-3)
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.AD_PACKAGE ON;
INSERT INTO dbo.AD_PACKAGE (PackageID, PackageName, PricePackage, PriceRoute, BasePriceClick, AdScore, Status) VALUES
(1, N'Gói cơ bản', 1000000.00, 200000.00, 5000.00, 50, N'Active'),
(2, N'Gói cao cấp', 2500000.00, 500000.00, 8000.00, 75, N'Active'),
(3, N'Gói VIP', 5000000.00, 1000000.00, 12000.00, 90, N'Active');
SET IDENTITY_INSERT dbo.AD_PACKAGE OFF;
GO

-- ══════════════════════════════════════════════════════════════
-- 22. AD_CAMPAIGN (7 record: ID 1-7)
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.AD_CAMPAIGN ON;
INSERT INTO dbo.AD_CAMPAIGN (AdCampaignID, PackageID, BrandID, RobotZoneID, CampaignName, StartDate, EndDate, Status) VALUES
(1, 1, 1, NULL, N'Coca mùa hè 2026', DATEADD(hour, 7, GETUTCDATE()), DATEADD(month, 3, DATEADD(hour, 7, GETUTCDATE())), N'Running'),
(2, 2, 2, 2, N'Clear Men tháng 6', DATEADD(day, -10, DATEADD(hour, 7, GETUTCDATE())), DATEADD(day, 20, DATEADD(hour, 7, GETUTCDATE())), N'Running'),
(3, 2, 3, 3, N'Vinamilk mùa hè 2026', DATEADD(day, -5, DATEADD(hour, 7, GETUTCDATE())), DATEADD(day, 60, DATEADD(hour, 7, GETUTCDATE())), N'Running'),
(4, 3, 4, 4, N'Sunhouse khuyến mãi T6', DATEADD(day, -15, DATEADD(hour, 7, GETUTCDATE())), DATEADD(day, 15, DATEADD(hour, 7, GETUTCDATE())), N'Running'),
(5, 2, 5, 5, N'Panasonic tháng này', DATEADD(day, -3, DATEADD(hour, 7, GETUTCDATE())), DATEADD(day, 30, DATEADD(hour, 7, GETUTCDATE())), N'Running'),
(6, 3, 3, 6, N'TH True Yogurt quảng bá', DATEADD(day, -7, DATEADD(hour, 7, GETUTCDATE())), DATEADD(day, 25, DATEADD(hour, 7, GETUTCDATE())), N'Running'),
(7, 2, 2, NULL, N'Dove Refresh Q3', DATEADD(month, 1, DATEADD(hour, 7, GETUTCDATE())), DATEADD(month, 4, DATEADD(hour, 7, GETUTCDATE())), N'Scheduled');
SET IDENTITY_INSERT dbo.AD_CAMPAIGN OFF;
GO

-- ══════════════════════════════════════════════════════════════
-- 23. SPONSORED_PRODUCT (13 record: ID 1-13)
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.SPONSORED_PRODUCT ON;
INSERT INTO dbo.SPONSORED_PRODUCT (SponsoredID, AdCampaignID, ProductID, Priority, status) VALUES
(1, 1, 3, 10, N'Active'),
(2, 2, 33, 8, N'Active'),
(3, 2, 43, 6, N'Active'),
(4, 3, 20, 10, N'Active'),
(5, 3, 22, 9, N'Active'),
(6, 3, 23, 7, N'Active'),
(7, 4, 30, 8, N'Active'),
(8, 4, 31, 6, N'Active'),
(9, 5, 32, 7, N'Active'),
(10, 5, 46, 5, N'Active'),
(11, 6, 23, 9, N'Active'),
(12, 6, 21, 7, N'Active'),
(13, 7, 34, 8, N'Active');
SET IDENTITY_INSERT dbo.SPONSORED_PRODUCT OFF;
GO

-- ══════════════════════════════════════════════════════════════
-- 24. AD_CAMPAIGN_LOG (30 record: ID 1-30)
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.AD_CAMPAIGN_LOG ON;
DECLARE @k INT = 1;
DECLARE @action NVARCHAR(50);
WHILE @k <= 30
BEGIN
    SET @action = CASE @k % 3 WHEN 0 THEN N'Click' WHEN 1 THEN N'View' ELSE N'RoutePass' END;
    INSERT INTO dbo.AD_CAMPAIGN_LOG (
        LogID, AdCampaignID, ActionType, ChargedAmount, Timestamp,
        SponsoredID, ProductID, RobotID, RobotZoneID, ZoneID, SlotID, MemberID, XCoord, YCoord)
    VALUES (
        @k,
        ((@k - 1) % 6) + 2,
        @action,
        CASE @action WHEN N'Click' THEN 8000.00 WHEN N'View' THEN 2000.00 ELSE 5000.00 END,
        DATEADD(hour, -(@k * 2), DATEADD(hour, 7, GETUTCDATE())),
        ((@k - 1) % 12) + 2,
        ((@k - 1) % 50) + 4,
        ((@k - 1) % 5) + 1,
        ((@k - 1) % 9) + 2,
        ((@k - 1) % 6) + 1,
        ((@k - 1) % 60) + 3,
        ((@k - 1) % 12) + 1,
        (@k * 5) % 100,
        (@k * 3) % 100
    );
    SET @k = @k + 1;
END
SET IDENTITY_INSERT dbo.AD_CAMPAIGN_LOG OFF;
GO

PRINT '✅ Restored ROBOT/BRAND/AD_* tables (5+10+20+5+3+7+13+30 records).';
GO
