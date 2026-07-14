-- ============================================================
-- SmartMarketBot — Minimal seed data for smoke test ERD V4.0
-- (Chạy SAU khi đã chạy erd_database.sql trên DB trống)
-- ============================================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- Clear existing data if any (để chạy lại an toàn)
DELETE FROM dbo.MEMBERHEALTH_PREFERENCE;
DELETE FROM dbo.PRODUCT_HEALTHTAG;
DELETE FROM dbo.PRODUCT_SLOT;
DELETE FROM dbo.SLOT;
DELETE FROM dbo.SHELF;
DELETE FROM dbo.AISLE;
DELETE FROM dbo.ZONE;
DELETE FROM dbo.FLOOR;
DELETE FROM dbo.SPONSORED_PRODUCT;
DELETE FROM dbo.AD_CAMPAIGN_LOG;
DELETE FROM dbo.AD_CAMPAIGN;
DELETE FROM dbo.AD_PACKAGE;
DELETE FROM dbo.BRAND;
DELETE FROM dbo.PRODUCT;
DELETE FROM dbo.PRODUCT_TYPE;
DELETE FROM dbo.SUBCATEGORY;
DELETE FROM dbo.CATEGORY;
DELETE FROM dbo.MEMBERSHIP;
DELETE FROM dbo.MEMBER;
DELETE FROM dbo.ACCOUNT;
DELETE FROM dbo.ROBOT;
GO

-- ─── 3. FLOOR + ZONE + AISLE + SHELF + SLOT (layout tối thiểu) ─
SET IDENTITY_INSERT dbo.FLOOR ON;
INSERT INTO dbo.FLOOR (FloorID, FloorNumber) VALUES (1, 1);
SET IDENTITY_INSERT dbo.FLOOR OFF;
GO

SET IDENTITY_INSERT dbo.ZONE ON;
INSERT INTO dbo.ZONE (ZoneID, FloorID, ZoneName, Description) VALUES
(1, 1, N'Khu rau củ', N'Tươi sống'),
(2, 1, N'Khu đồ uống', N'Nước giải khát');
SET IDENTITY_INSERT dbo.ZONE OFF;
GO

SET IDENTITY_INSERT dbo.AISLE ON;
INSERT INTO dbo.AISLE (AisleID, ZoneID, AisleCode, AisleName) VALUES
(1, 1, N'A01', N'Dãy rau lá'),
(2, 2, N'B01', N'Dãy nước ngọt');
SET IDENTITY_INSERT dbo.AISLE OFF;
GO

SET IDENTITY_INSERT dbo.SHELF ON;
INSERT INTO dbo.SHELF (ShelfID, AisleID, LevelNumber) VALUES
(1, 1, 1),
(2, 2, 1);
SET IDENTITY_INSERT dbo.SHELF OFF;
GO

SET IDENTITY_INSERT dbo.SLOT ON;
INSERT INTO dbo.SLOT (SlotID, ShelfID, SlotCode, Quantity, LastScannedAt) VALUES
(1, 1, N'S01', 10, NULL),
(2, 2, N'S02', 50, NULL);
SET IDENTITY_INSERT dbo.SLOT OFF;
GO

-- ─── 4. CATEGORY + SUBCATEGORY + PRODUCT_TYPE ─────────────────
SET IDENTITY_INSERT dbo.CATEGORY ON;
INSERT INTO dbo.CATEGORY (CategoryID, CategoryName) VALUES
(1, N'Thực phẩm tươi sống'),
(2, N'Đồ uống');
SET IDENTITY_INSERT dbo.CATEGORY OFF;
GO

SET IDENTITY_INSERT dbo.SUBCATEGORY ON;
INSERT INTO dbo.SUBCATEGORY (SubcategoryID, CategoryID, SubcategoryName) VALUES
(1, 1, N'Rau xanh'),
(2, 2, N'Nước ngọt');
SET IDENTITY_INSERT dbo.SUBCATEGORY OFF;
GO

SET IDENTITY_INSERT dbo.PRODUCT_TYPE ON;
INSERT INTO dbo.PRODUCT_TYPE (ProductTypeID, SubcategoryID, TypeName) VALUES
(1, 1, N'Rau lá xanh'),
(2, 2, N'Nước có ga');
SET IDENTITY_INSERT dbo.PRODUCT_TYPE OFF;
GO

-- ─── 5. PRODUCT (3 sản phẩm mẫu) ─────────────────────────────
SET IDENTITY_INSERT dbo.PRODUCT ON;
INSERT INTO dbo.PRODUCT (ProductID, ProductTypeID, ProductName, UnitPrice, PromotionPrice, ExpiredDate, ImageUrl, WeightOrVolume, Unit, Description, Status, SubstituteProductID) VALUES
(1, 1, N'Rau muống', 8000.00, NULL, NULL, NULL, 1.000, N'kg', N'Rau muống tươi', N'Available', NULL),
(2, 1, N'Cải bó xôi', 12000.00, NULL, NULL, NULL, 0.500, N'kg', N'Cải bó xôi Đà Lạt', N'Available', NULL),
(3, 2, N'Coca Cola', 10000.00, NULL, NULL, NULL, 330.000, N'ml', N'Nước ngọt có ga', N'Available', NULL);
SET IDENTITY_INSERT dbo.PRODUCT OFF;
GO

-- ─── 5.1 PRODUCT_SLOT (Liên kết sản phẩm vào Slot kệ) ──────────
INSERT INTO dbo.PRODUCT_SLOT (SlotID, ProductID) VALUES
(1, 1),
(1, 2),
(2, 3);
GO

-- ─── 6. BRAND (5 brand mẫu) ───────────────────────────────────
SET IDENTITY_INSERT dbo.BRAND ON;
INSERT INTO dbo.BRAND (BrandID, BrandName, Wallet, Description) VALUES
(1, N'Coca-Cola Company', 1000000.00, N'Hãng nước giải khát'),
(2, N'PepsiCo Vietnam', 500000.00, N'Đồ uống giải khát'),
(3, N'Nestle Vietnam', 2000000.00, N'Sữa và thực phẩm'),
(4, N'Vinamilk', 3000000.00, N'Sữa tươi và sản phẩm từ sữa'),
(5, N'Unilever Vietnam', 1500000.00, N'Hàng tiêu dùng nhanh');
SET IDENTITY_INSERT dbo.BRAND OFF;
GO

-- ─── 7. AD_PACKAGE + AD_CAMPAIGN (1 gói + 1 chiến dịch) ──────
SET IDENTITY_INSERT dbo.AD_PACKAGE ON;
INSERT INTO dbo.AD_PACKAGE (PackageID, PackageName, PricePackage, PriceRoute, BasePriceClick, AdScore, Status) VALUES
(1, N'Gói cơ bản', 1000000.00, 200000.00, 5000.00, 50, N'Active');
SET IDENTITY_INSERT dbo.AD_PACKAGE OFF;
GO

SET IDENTITY_INSERT dbo.AD_CAMPAIGN ON;
INSERT INTO dbo.AD_CAMPAIGN (AdCampaignID, PackageID, BrandID, SemanticObjectID, CampaignName, StartDate, EndDate, Status) VALUES
(1, 1, 1, NULL, N'Coca mùa hè 2026', DATEADD(hour, 7, GETUTCDATE()), DATEADD(month, 3, DATEADD(hour, 7, GETUTCDATE())), N'Inactive');
SET IDENTITY_INSERT dbo.AD_CAMPAIGN OFF;
GO

-- ─── 8. SPONSORED_PRODUCT (1 sản phẩm tài trợ) ──────────────
SET IDENTITY_INSERT dbo.SPONSORED_PRODUCT ON;
INSERT INTO dbo.SPONSORED_PRODUCT (SponsoredID, AdCampaignID, ProductID, Priority, status) VALUES
(1, 1, 3, 10, N'Active');
SET IDENTITY_INSERT dbo.SPONSORED_PRODUCT OFF;
GO

-- ─── 9. HEALTH_TAG + MEMBERHEALTH_PREFERENCE (test allergy) ──
SET IDENTITY_INSERT dbo.HEALTH_TAG ON;
INSERT INTO dbo.HEALTH_TAG (HealthTagID, TagName, TagType) VALUES
(1, N'Đậu phộng', N'allergy'),
(2, N'Gluten', N'allergy');
SET IDENTITY_INSERT dbo.HEALTH_TAG OFF;
GO

GO

PRINT '✅ Seed data inserted successfully.';
GO
