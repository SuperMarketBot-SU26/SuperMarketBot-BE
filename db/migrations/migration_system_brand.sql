-- ============================================================================
-- SmartMarketBot — Migration: System Brand
-- Thêm cột IsSystemBrand vào bảng BRAND + Seed Brand "SmartMart"
--
-- Cách dùng:
--   sqlcmd -S <server> -d SuperMarketBot -i db/migrations/migration_system_brand.sql
-- ============================================================================

USE SuperMarketBot;
GO

PRINT '=== System Brand Migration START ===';
GO

-- ── Thêm cột IsSystemBrand ────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.BRAND') AND name = 'IsSystemBrand'
)
BEGIN
    ALTER TABLE dbo.BRAND ADD IsSystemBrand BIT NOT NULL DEFAULT 0;
    PRINT '[1/2] Added column BRAND.IsSystemBrand (default = 0)';
END
ELSE
    PRINT '[1/2] BRAND.IsSystemBrand already exists — skipped';
GO

-- ── Seed Brand "SmartMart" (Brand dành riêng cho siêu thị) ───────────────
-- BrandID = 99 để tránh xung đột với các brand bên ngoài (1–5).
-- Wallet = 0 vì siêu thị không cần nạp tiền (không bị trừ khi Activate).
-- IsSystemBrand = 1 → Skip Package fee + Skip click charge khi chạy campaign.
IF NOT EXISTS (SELECT 1 FROM dbo.BRAND WHERE BrandID = 99)
BEGIN
    SET IDENTITY_INSERT dbo.BRAND ON;
    INSERT INTO dbo.BRAND (BrandID, BrandName, Wallet, Description, IsSystemBrand) VALUES
    (99, N'SmartMart', 0.00, N'Siêu thị SmartMart — Brand hệ thống tự chạy khuyến mãi (miễn phí Package & Click)', 1);
    SET IDENTITY_INSERT dbo.BRAND OFF;
    PRINT '[2/2] Seeded BrandID=99: SmartMart (SystemBrand)';
END
ELSE
    PRINT '[2/2] BrandID=99 already exists — skipped';
GO

PRINT '=== System Brand Migration DONE ===';
GO
