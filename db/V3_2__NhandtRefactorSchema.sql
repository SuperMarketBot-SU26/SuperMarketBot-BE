/*
  V3.2 — Nhàn DT Refactor Schema (Idempotent — chạy lại an toàn)
  Áp dụng trên DB SuperMarketBot đã có V3.0 + V3.1.

  Thay đổi chính:
    - Xóa: Supermarkets, Payments, UserRoles, Roles
    - Đổi tên: Users → Accounts (thêm cột Role INT enum)
    - Đổi tên cột: UserID → AccountID trong Accounts, Members, Staff, Admins, UserTokens
    - Tạo mới: Brands, AdPackages
    - Cập nhật: SponsoredProducts (thay cột cũ bằng BrandID + PackageID FK)
*/

USE SuperMarketBot;
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================
-- BƯỚC 1: Xóa Payments (phụ thuộc Users trước khi đổi tên)
-- ============================================================

IF OBJECT_ID('dbo.Payments', 'U') IS NOT NULL
BEGIN
    -- Xóa index trước
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Payments_OrderCode' AND object_id = OBJECT_ID('dbo.Payments'))
        DROP INDEX IX_Payments_OrderCode ON dbo.Payments;
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Payments_UserId_Status' AND object_id = OBJECT_ID('dbo.Payments'))
        DROP INDEX IX_Payments_UserId_Status ON dbo.Payments;
    DROP TABLE dbo.Payments;
    PRINT 'DROPPED: Payments';
END
GO

-- ============================================================
-- BƯỚC 2: Xóa UserRoles và Roles
-- ============================================================

IF OBJECT_ID('dbo.UserRoles', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.UserRoles;
    PRINT 'DROPPED: UserRoles';
END
GO

IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Roles;
    PRINT 'DROPPED: Roles';
END
GO

-- ============================================================
-- BƯỚC 3: Xóa liên kết Floors → Supermarkets rồi xóa Supermarkets
-- ============================================================

-- Tìm và xóa FK constraint của Floors → Supermarkets
DECLARE @fkFloors NVARCHAR(200);
SELECT @fkFloors = fk.name
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
WHERE fk.parent_object_id = OBJECT_ID('dbo.Floors')
  AND c.name = 'SupermarketID';

IF @fkFloors IS NOT NULL
    EXEC ('ALTER TABLE dbo.Floors DROP CONSTRAINT [' + @fkFloors + ']');
GO

-- Xóa cột SupermarketID khỏi Floors (nếu còn)
IF COL_LENGTH('dbo.Floors', 'SupermarketID') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Floors DROP COLUMN SupermarketID;
    PRINT 'DROPPED COLUMN: Floors.SupermarketID';
END
GO

-- Xóa bảng Supermarkets
IF OBJECT_ID('dbo.Supermarkets', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Supermarkets;
    PRINT 'DROPPED: Supermarkets';
END
GO

-- ============================================================
-- BƯỚC 4: Đổi tên Users → Accounts
-- ============================================================

IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL AND OBJECT_ID('dbo.Accounts', 'U') IS NULL
BEGIN
    EXEC sp_rename 'dbo.Users', 'Accounts';
    PRINT 'RENAMED: Users → Accounts';
END
GO

-- Đổi tên cột UserID → AccountID trong Accounts
IF COL_LENGTH('dbo.Accounts', 'UserID') IS NOT NULL AND COL_LENGTH('dbo.Accounts', 'AccountID') IS NULL
BEGIN
    EXEC sp_rename 'dbo.Accounts.UserID', 'AccountID', 'COLUMN';
    PRINT 'RENAMED COLUMN: Accounts.UserID → AccountID';
END
GO

-- Thêm cột Role (1=Admin, 2=Staff, 3=Member) — mặc định là 3 (Member)
IF COL_LENGTH('dbo.Accounts', 'Role') IS NULL
BEGIN
    ALTER TABLE dbo.Accounts ADD Role INT NOT NULL CONSTRAINT DF_Accounts_Role DEFAULT 3;
    PRINT 'ADDED COLUMN: Accounts.Role';
END
GO

-- Seed Role dựa trên thông tin UserRoles cũ đã mất → dùng Username để suy luận
UPDATE dbo.Accounts SET Role = 1 WHERE Username LIKE 'admin_%';
UPDATE dbo.Accounts SET Role = 2 WHERE Username LIKE 'staff_%';
-- Member là mặc định 3 — không cần UPDATE
GO

-- Đảm bảo các cột auth từ V3.0 tồn tại
IF COL_LENGTH('dbo.Accounts', 'EmailConfirmed') IS NULL
    ALTER TABLE dbo.Accounts ADD EmailConfirmed BIT NOT NULL CONSTRAINT DF_Accounts_EmailConfirmed DEFAULT 0;
GO
IF COL_LENGTH('dbo.Accounts', 'FullName') IS NULL
    ALTER TABLE dbo.Accounts ADD FullName NVARCHAR(100) NULL;
GO
IF COL_LENGTH('dbo.Accounts', 'AvatarUrl') IS NULL
    ALTER TABLE dbo.Accounts ADD AvatarUrl NVARCHAR(500) NULL;
GO
IF COL_LENGTH('dbo.Accounts', 'UpdatedAt') IS NULL
    ALTER TABLE dbo.Accounts ADD UpdatedAt DATETIME2 NULL;
GO

-- Tạo unique index trên Email (nếu chưa có)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Accounts_Email' AND object_id = OBJECT_ID('dbo.Accounts'))
    EXEC('CREATE UNIQUE NONCLUSTERED INDEX IX_Accounts_Email ON dbo.Accounts(Email) WHERE Email IS NOT NULL');
GO

-- ============================================================
-- BƯỚC 5: Cập nhật bảng Members: UserID → AccountID
-- ============================================================

-- Tìm và xóa FK constraint Members → Users (giờ là Accounts)
DECLARE @fkMembers NVARCHAR(200);
SELECT @fkMembers = fk.name
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
WHERE fk.parent_object_id = OBJECT_ID('dbo.Members') AND c.name = 'UserID';

IF @fkMembers IS NOT NULL
    EXEC ('ALTER TABLE dbo.Members DROP CONSTRAINT [' + @fkMembers + ']');
GO

-- Đổi tên cột
IF COL_LENGTH('dbo.Members', 'UserID') IS NOT NULL AND COL_LENGTH('dbo.Members', 'AccountID') IS NULL
BEGIN
    EXEC sp_rename 'dbo.Members.UserID', 'AccountID', 'COLUMN';
    PRINT 'RENAMED COLUMN: Members.UserID → AccountID';
END
GO

-- Thêm lại FK constraint
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys fk
    JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
    JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
    WHERE fk.parent_object_id = OBJECT_ID('dbo.Members') AND c.name = 'AccountID')
BEGIN
    ALTER TABLE dbo.Members
        ADD CONSTRAINT FK_Members_Accounts_AccountID
        FOREIGN KEY (AccountID) REFERENCES dbo.Accounts(AccountID) ON DELETE SET NULL;
    PRINT 'ADDED FK: Members.AccountID → Accounts';
END
GO

-- ============================================================
-- BƯỚC 6: Cập nhật bảng Staff: UserID → AccountID
-- ============================================================

DECLARE @fkStaff NVARCHAR(200);
SELECT @fkStaff = fk.name
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
WHERE fk.parent_object_id = OBJECT_ID('dbo.Staff') AND c.name = 'UserID';

IF @fkStaff IS NOT NULL
    EXEC ('ALTER TABLE dbo.Staff DROP CONSTRAINT [' + @fkStaff + ']');
GO

IF COL_LENGTH('dbo.Staff', 'UserID') IS NOT NULL AND COL_LENGTH('dbo.Staff', 'AccountID') IS NULL
BEGIN
    EXEC sp_rename 'dbo.Staff.UserID', 'AccountID', 'COLUMN';
    PRINT 'RENAMED COLUMN: Staff.UserID → AccountID';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys fk
    JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
    JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
    WHERE fk.parent_object_id = OBJECT_ID('dbo.Staff') AND c.name = 'AccountID')
BEGIN
    ALTER TABLE dbo.Staff
        ADD CONSTRAINT FK_Staff_Accounts_AccountID
        FOREIGN KEY (AccountID) REFERENCES dbo.Accounts(AccountID) ON DELETE CASCADE;
    PRINT 'ADDED FK: Staff.AccountID → Accounts';
END
GO

-- ============================================================
-- BƯỚC 7: Cập nhật bảng Admins: UserID → AccountID
-- ============================================================

DECLARE @fkAdmins NVARCHAR(200);
SELECT @fkAdmins = fk.name
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
WHERE fk.parent_object_id = OBJECT_ID('dbo.Admins') AND c.name = 'UserID';

IF @fkAdmins IS NOT NULL
    EXEC ('ALTER TABLE dbo.Admins DROP CONSTRAINT [' + @fkAdmins + ']');
GO

IF COL_LENGTH('dbo.Admins', 'UserID') IS NOT NULL AND COL_LENGTH('dbo.Admins', 'AccountID') IS NULL
BEGIN
    EXEC sp_rename 'dbo.Admins.UserID', 'AccountID', 'COLUMN';
    PRINT 'RENAMED COLUMN: Admins.UserID → AccountID';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys fk
    JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
    JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
    WHERE fk.parent_object_id = OBJECT_ID('dbo.Admins') AND c.name = 'AccountID')
BEGIN
    ALTER TABLE dbo.Admins
        ADD CONSTRAINT FK_Admins_Accounts_AccountID
        FOREIGN KEY (AccountID) REFERENCES dbo.Accounts(AccountID) ON DELETE CASCADE;
    PRINT 'ADDED FK: Admins.AccountID → Accounts';
END
GO

-- ============================================================
-- BƯỚC 8: Cập nhật bảng UserTokens: UserId → AccountId
-- ============================================================

-- Xóa index cũ trước khi đổi tên cột
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserTokens_UserId_IsRevoked' AND object_id = OBJECT_ID('dbo.UserTokens'))
    DROP INDEX IX_UserTokens_UserId_IsRevoked ON dbo.UserTokens;
GO

DECLARE @fkTokens NVARCHAR(200);
SELECT @fkTokens = fk.name
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
WHERE fk.parent_object_id = OBJECT_ID('dbo.UserTokens') AND c.name IN ('UserId', 'AccountId');

IF @fkTokens IS NOT NULL
    EXEC ('ALTER TABLE dbo.UserTokens DROP CONSTRAINT [' + @fkTokens + ']');
GO

IF COL_LENGTH('dbo.UserTokens', 'UserId') IS NOT NULL AND COL_LENGTH('dbo.UserTokens', 'AccountId') IS NULL
BEGIN
    EXEC sp_rename 'dbo.UserTokens.UserId', 'AccountId', 'COLUMN';
    PRINT 'RENAMED COLUMN: UserTokens.UserId → AccountId';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys fk
    JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
    JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
    WHERE fk.parent_object_id = OBJECT_ID('dbo.UserTokens') AND c.name = 'AccountId')
BEGIN
    ALTER TABLE dbo.UserTokens
        ADD CONSTRAINT FK_UserTokens_Accounts_AccountId
        FOREIGN KEY (AccountId) REFERENCES dbo.Accounts(AccountID) ON DELETE CASCADE;
    PRINT 'ADDED FK: UserTokens.AccountId → Accounts';
END
GO

-- Tạo lại index với tên mới
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserTokens_AccountId_IsRevoked' AND object_id = OBJECT_ID('dbo.UserTokens'))
    CREATE NONCLUSTERED INDEX IX_UserTokens_AccountId_IsRevoked ON dbo.UserTokens(AccountId, IsRevoked);
GO

-- ============================================================
-- BƯỚC 9: Tạo bảng Brands
-- ============================================================

IF OBJECT_ID('dbo.Brands', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Brands (
        BrandID     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        BrandName   NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NULL
    );
    PRINT 'CREATED: Brands';
END
GO

-- Seed dữ liệu Brands từ SponsorBrand cũ (nếu chưa có seed)
IF NOT EXISTS (SELECT 1 FROM dbo.Brands)
BEGIN
    -- Trích xuất tên nhãn hàng duy nhất từ SponsoredProducts cũ
    INSERT INTO dbo.Brands (BrandName, Description)
    SELECT DISTINCT SponsorBrand, N'Nhãn hàng tài trợ quảng cáo'
    FROM dbo.SponsoredProducts
    WHERE SponsorBrand IS NOT NULL;

    -- Nếu không có dữ liệu cũ, thêm mẫu
    IF NOT EXISTS (SELECT 1 FROM dbo.Brands WHERE BrandName = N'Orion Vina')
        INSERT INTO dbo.Brands (BrandName, Description) VALUES
        (N'Orion Vina', N'Tập đoàn bánh kẹo Orion Vina Hàn Quốc'),
        (N'TH True Milk', N'Công ty cổ phần thực phẩm sữa TH'),
        (N'Vinamilk', N'Công ty cổ phần sữa Việt Nam');
    PRINT 'SEEDED: Brands';
END
GO

-- ============================================================
-- BƯỚC 10: Tạo bảng AdPackages
-- ============================================================

IF OBJECT_ID('dbo.AdPackages', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AdPackages (
        PackageID     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PackageName   NVARCHAR(100) NOT NULL,
        Price         DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        AdScore       INT NOT NULL DEFAULT 0,
        TimeSlotStart TIME NULL,
        TimeSlotEnd   TIME NULL,
        IsWeekendOnly BIT NOT NULL DEFAULT 0
    );
    PRINT 'CREATED: AdPackages';
END
GO

-- Seed AdPackages mẫu
IF NOT EXISTS (SELECT 1 FROM dbo.AdPackages)
BEGIN
    INSERT INTO dbo.AdPackages (PackageName, Price, AdScore, TimeSlotStart, TimeSlotEnd, IsWeekendOnly) VALUES
    (N'Gói Sáng Sớm (07:00-12:00)', 500000.00, 85, '07:00:00', '12:00:00', 0),
    (N'Gói Cả Ngày (All Day)', 800000.00, 70, NULL, NULL, 0),
    (N'Gói Cuối Tuần', 1200000.00, 100, NULL, NULL, 1),
    (N'Gói Giờ Vàng (17:00-21:00)', 700000.00, 90, '17:00:00', '21:00:00', 0);
    PRINT 'SEEDED: AdPackages';
END
GO

-- ============================================================
-- BƯỚC 11: Cập nhật SponsoredProducts → thêm BrandID + PackageID
-- ============================================================

-- Thêm cột BrandID (nullable trước khi có dữ liệu)
IF COL_LENGTH('dbo.SponsoredProducts', 'BrandID') IS NULL
BEGIN
    ALTER TABLE dbo.SponsoredProducts ADD BrandID INT NULL;
    PRINT 'ADDED COLUMN: SponsoredProducts.BrandID';
END
GO

-- Thêm cột PackageID
IF COL_LENGTH('dbo.SponsoredProducts', 'PackageID') IS NULL
BEGIN
    ALTER TABLE dbo.SponsoredProducts ADD PackageID INT NULL;
    PRINT 'ADDED COLUMN: SponsoredProducts.PackageID';
END
GO

-- Điền BrandID từ SponsorBrand (khớp với Brands vừa tạo)
UPDATE sp
SET sp.BrandID = b.BrandID
FROM dbo.SponsoredProducts sp
INNER JOIN dbo.Brands b ON b.BrandName = sp.SponsorBrand
WHERE sp.BrandID IS NULL;

-- Nếu vẫn còn NULL (không khớp tên) → gán Brand đầu tiên
UPDATE dbo.SponsoredProducts
SET BrandID = (SELECT TOP 1 BrandID FROM dbo.Brands ORDER BY BrandID)
WHERE BrandID IS NULL;
GO

-- Điền PackageID dựa theo AdScore + TimeSlot cũ
UPDATE sp
SET sp.PackageID = (
    SELECT TOP 1 ap.PackageID
    FROM dbo.AdPackages ap
    WHERE (sp.TimeSlotStart IS NULL OR ap.TimeSlotStart = sp.TimeSlotStart)
      AND (sp.IsWeekendOnly = ap.IsWeekendOnly)
    ORDER BY ABS(ap.AdScore - sp.AdScore)
)
FROM dbo.SponsoredProducts sp
WHERE sp.PackageID IS NULL;

-- Fallback nếu vẫn còn NULL
UPDATE dbo.SponsoredProducts
SET PackageID = (SELECT TOP 1 PackageID FROM dbo.AdPackages ORDER BY PackageID)
WHERE PackageID IS NULL;
GO

-- Bây giờ thêm FK constraints (sau khi đã fill dữ liệu)
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SponsoredProducts_Brands_BrandID'
      AND parent_object_id = OBJECT_ID('dbo.SponsoredProducts'))
BEGIN
    ALTER TABLE dbo.SponsoredProducts
        ADD CONSTRAINT FK_SponsoredProducts_Brands_BrandID
        FOREIGN KEY (BrandID) REFERENCES dbo.Brands(BrandID);
    PRINT 'ADDED FK: SponsoredProducts.BrandID → Brands';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SponsoredProducts_AdPackages_PackageID'
      AND parent_object_id = OBJECT_ID('dbo.SponsoredProducts'))
BEGIN
    ALTER TABLE dbo.SponsoredProducts
        ADD CONSTRAINT FK_SponsoredProducts_AdPackages_PackageID
        FOREIGN KEY (PackageID) REFERENCES dbo.AdPackages(PackageID);
    PRINT 'ADDED FK: SponsoredProducts.PackageID → AdPackages';
END
GO

-- Đổi BrandID và PackageID từ nullable → NOT NULL
IF COL_LENGTH('dbo.SponsoredProducts', 'BrandID') IS NOT NULL
BEGIN
    ALTER TABLE dbo.SponsoredProducts ALTER COLUMN BrandID INT NOT NULL;
    ALTER TABLE dbo.SponsoredProducts ALTER COLUMN PackageID INT NOT NULL;
    PRINT 'MADE NOT NULL: SponsoredProducts.BrandID, PackageID';
END
GO

-- Xóa các cột quảng cáo cũ (đã chuyển sang AdPackages)
IF COL_LENGTH('dbo.SponsoredProducts', 'SponsorBrand') IS NOT NULL
    ALTER TABLE dbo.SponsoredProducts DROP COLUMN SponsorBrand;
IF COL_LENGTH('dbo.SponsoredProducts', 'AdScore') IS NOT NULL
    ALTER TABLE dbo.SponsoredProducts DROP COLUMN AdScore;
IF COL_LENGTH('dbo.SponsoredProducts', 'TimeSlotStart') IS NOT NULL
    ALTER TABLE dbo.SponsoredProducts DROP COLUMN TimeSlotStart;
IF COL_LENGTH('dbo.SponsoredProducts', 'TimeSlotEnd') IS NOT NULL
    ALTER TABLE dbo.SponsoredProducts DROP COLUMN TimeSlotEnd;
IF COL_LENGTH('dbo.SponsoredProducts', 'IsWeekendOnly') IS NOT NULL
    ALTER TABLE dbo.SponsoredProducts DROP COLUMN IsWeekendOnly;
IF COL_LENGTH('dbo.SponsoredProducts', 'BidPrice') IS NOT NULL
    ALTER TABLE dbo.SponsoredProducts DROP COLUMN BidPrice;
IF COL_LENGTH('dbo.SponsoredProducts', 'WeekendMultiplier') IS NOT NULL
    ALTER TABLE dbo.SponsoredProducts DROP COLUMN WeekendMultiplier;
PRINT 'DROPPED OLD COLUMNS: SponsoredProducts ad config columns';
GO

-- ============================================================
-- HOÀN TẤT
-- ============================================================
PRINT '====================================================================';
PRINT '  SUCCESS: V3.2 Nhàn DT Refactor Schema applied!';
PRINT '  37 Tables | Accounts + Role enum | Brands + AdPackages';
PRINT '====================================================================';
GO
