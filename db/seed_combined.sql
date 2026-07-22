-- ============================================================
-- SmartMarketBot — Combined Seed (ERD v4 base + FE dev data)
-- Gộp: seed_erd_v4 + seed_fe_dev_fix1/2/3
-- Fix: bỏ cột AdCampaignID khỏi INSERT PRODUCT (không tồn tại)
-- Chạy AFTER dotnet ef database update
-- ============================================================

USE SuperMarketBot;
GO

SET NOCOUNT ON;
GO

-- ═══════════════════════════════════════════════════════════
-- CLEAR ALL DATA (theo thứ tự FK ngược)
-- ═══════════════════════════════════════════════════════════
DELETE FROM dbo.AISLE_SCAN;
DELETE FROM dbo.SEMANTIC_OBJECT;
DELETE FROM dbo.ROUTE_ASSIGNMENT;
DELETE FROM dbo.ROUTE_NODE_MAPPING;
DELETE FROM dbo.ROBOT_ROUTE;
DELETE FROM dbo.AISLE_NODE;
DELETE FROM dbo.NAVIGATION_EDGE;
DELETE FROM dbo.NAVIGATION_NODE;
DELETE FROM dbo.MAP;
DELETE FROM dbo.AD_CAMPAIGN_ROUTE;
DELETE FROM dbo.AD_CAMPAIGN_ZONE;
DELETE FROM dbo.AD_CAMPAIGN_LOG;
DELETE FROM dbo.SPONSORED_PRODUCT;
DELETE FROM dbo.AD_RESOURCE;
DELETE FROM dbo.AD_CAMPAIGN;
DELETE FROM dbo.AD_PACKAGE;
DELETE FROM dbo.BRAND;
DELETE FROM dbo.ROBOT_ZONE;
DELETE FROM dbo.ROBOT_LOG;
DELETE FROM dbo.ROBOT;
DELETE FROM dbo.CART_ITEM;
DELETE FROM dbo.CART;
DELETE FROM dbo.INVOICE_HISTORY_ITEM;
DELETE FROM dbo.INVOICE_HISTORY;
DELETE FROM dbo.MEAL_ITEM;
DELETE FROM dbo.MEAL_SUGGESTION;
DELETE FROM dbo.MEMBERHEALTH_PREFERENCE;
DELETE FROM dbo.PRODUCT_HEALTHTAG;
DELETE FROM dbo.HEALTH_TAG;
DELETE FROM dbo.PRODUCT_SLOT;
DELETE FROM dbo.SLOT;
DELETE FROM dbo.SHELF;
DELETE FROM dbo.AISLE;
DELETE FROM dbo.ZONE;
DELETE FROM dbo.FLOOR;
DELETE FROM dbo.MEMBERSHIP;
DELETE FROM dbo.MEMBER;
DELETE FROM dbo.PRODUCT;
DELETE FROM dbo.PRODUCT_TYPE;
DELETE FROM dbo.SUBCATEGORY;
DELETE FROM dbo.CATEGORY;
DELETE FROM dbo.ACCOUNT;
GO

-- Reset IDENTITY counters
DBCC CHECKIDENT('dbo.ACCOUNT', RESEED, 0);
DBCC CHECKIDENT('dbo.MEMBER', RESEED, 0);
DBCC CHECKIDENT('dbo.MEMBERSHIP', RESEED, 0);
DBCC CHECKIDENT('dbo.HEALTH_TAG', RESEED, 0);
DBCC CHECKIDENT('dbo.CATEGORY', RESEED, 0);
DBCC CHECKIDENT('dbo.SUBCATEGORY', RESEED, 0);
DBCC CHECKIDENT('dbo.PRODUCT_TYPE', RESEED, 0);
DBCC CHECKIDENT('dbo.PRODUCT', RESEED, 0);
DBCC CHECKIDENT('dbo.FLOOR', RESEED, 0);
DBCC CHECKIDENT('dbo.ZONE', RESEED, 0);
DBCC CHECKIDENT('dbo.AISLE', RESEED, 0);
DBCC CHECKIDENT('dbo.SHELF', RESEED, 0);
DBCC CHECKIDENT('dbo.SLOT', RESEED, 0);
DBCC CHECKIDENT('dbo.PRODUCT_SLOT', RESEED, 0);
DBCC CHECKIDENT('dbo.ROBOT', RESEED, 0);
DBCC CHECKIDENT('dbo.ROBOT_LOG', RESEED, 0);
DBCC CHECKIDENT('dbo.ROBOT_ZONE', RESEED, 0);
DBCC CHECKIDENT('dbo.BRAND', RESEED, 0);
DBCC CHECKIDENT('dbo.AD_PACKAGE', RESEED, 0);
DBCC CHECKIDENT('dbo.AD_CAMPAIGN', RESEED, 0);
DBCC CHECKIDENT('dbo.SPONSORED_PRODUCT', RESEED, 0);
DBCC CHECKIDENT('dbo.AD_CAMPAIGN_LOG', RESEED, 0);
DBCC CHECKIDENT('dbo.INVOICE_HISTORY', RESEED, 0);
DBCC CHECKIDENT('dbo.INVOICE_HISTORY_ITEM', RESEED, 0);
DBCC CHECKIDENT('dbo.MEAL_SUGGESTION', RESEED, 0);
DBCC CHECKIDENT('dbo.CART', RESEED, 0);
DBCC CHECKIDENT('dbo.CART_ITEM', RESEED, 0);
GO

-- ═══════════════════════════════════════════════════════════
-- 1. ACCOUNT (7 accounts: admin/staff/member)
-- ═══════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.ACCOUNT ON;
INSERT INTO dbo.ACCOUNT (AccountID, Username, PasswordHash, Email, Phone, FullName, Status, Role, OtpCode, OtpExpiredAt, OtpType, CreatedAt) VALUES
(1, N'admin', N'pbkdf2$100000$YWFhYWFhYWFhYWFhYWFhYQ==$YjJiMmMzZDRlNWU2ZTcwZjE4MzQ1Njc4OTAxMjM0NTY3', N'admin@smartmarket.local', N'0900000001', N'System Admin', N'Active', N'Admin', NULL, NULL, NULL, DATEADD(hour, 7, GETUTCDATE())),
(2, N'staff', N'pbkdf2$100000$YWFhYWFhYWFhYWFhYWFhYQ==$YjJiMmMzZDRlNWU2ZTcwZjE4MzQ1Njc4OTAxMjM0NTY3', N'staff@smartmarket.local', N'0900000002', N'Nguyễn Văn Khoa', N'Active', N'Staff', NULL, NULL, NULL, DATEADD(hour, 7, GETUTCDATE())),
(3, N'member1', N'pbkdf2$100000$YWFhYWFhYWFhYWFhYWFhYQ==$YjJiMmMzZDRlNWU2ZTcwZjE4MzQ1Njc4OTAxMjM0NTY3', N'member1@smartmarket.local', N'0900000003', N'Nguyễn Văn A', N'Active', N'Member', NULL, NULL, NULL, DATEADD(hour, 7, GETUTCDATE())),
(4, N'member2', N'pbkdf2$100000$YWFhYWFhYWFhYWFhYWFhYQ==$YjJiMmMzZDRlNWU2ZTcwZjE4MzQ1Njc4OTAxMjM0NTY3', N'member2@smartmarket.local', N'0900000004', N'Trần Thị Bình', N'Active', N'Member', NULL, NULL, NULL, DATEADD(hour, 7, GETUTCDATE())),
(5, N'member3', N'pbkdf2$100000$YWFhYWFhYWFhYWFhYWFhYQ==$YjJiMmMzZDRlNWU2ZTcwZjE4MzQ1Njc4OTAxMjM0NTY3', N'member3@smartmarket.local', N'0900000005', N'Lê Minh Cường', N'Active', N'Member', NULL, NULL, NULL, DATEADD(hour, 7, GETUTCDATE())),
(6, N'staff2', N'pbkdf2$100000$YWFhYWFhYWFhYWFhYWFhYQ==$YjJiMmMzZDRlNWU2ZTcwZjE4MzQ1Njc4OTAxMjM0NTY3', N'staff2@smartmarket.local', N'0900000006', N'Phạm Thị Dung', N'Active', N'Staff', NULL, NULL, NULL, DATEADD(hour, 7, GETUTCDATE())),
(7, N'admin2', N'pbkdf2$100000$YWFhYWFhYWFhYWFhYWFhYQ==$YjJiMmMzZDRlNWU2ZTcwZjE4MzQ1Njc4OTAxMjM0NTY3', N'admin2@smartmarket.local', N'0900000007', N'Võ Hoàng Nam', N'Active', N'Admin', NULL, NULL, NULL, DATEADD(hour, 7, GETUTCDATE()));
SET IDENTITY_INSERT dbo.ACCOUNT OFF;
GO

-- ═══════════════════════════════════════════════════════════
-- 2. MEMBER (4 members)
-- ═══════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.MEMBER ON;
INSERT INTO dbo.MEMBER (MemberID, AccountID, FullName, FacePath, FaceVector, SpendingLimit, TotalPoints) VALUES
(1, 3, N'Nguyễn Văn A', NULL, NULL, 500000.00, 100),
(2, 4, N'Trần Thị Bình', NULL, NULL, 300000.00, 50),
(3, 5, N'Lê Minh Cường', NULL, NULL, 1000000.00, 200),
(4, NULL, N'Khách vãng lai', NULL, NULL, 0.00, 0);
SET IDENTITY_INSERT dbo.MEMBER OFF;
GO

-- ═══════════════════════════════════════════════════════════
-- 3. FLOOR + ZONE + AISLE + SHELF + SLOT
-- ═══════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.FLOOR ON;
INSERT INTO dbo.FLOOR (FloorID, FloorNumber) VALUES (1, 1);
SET IDENTITY_INSERT dbo.FLOOR OFF;
GO

SET IDENTITY_INSERT dbo.ZONE ON;
INSERT INTO dbo.ZONE (ZoneID, FloorID, ZoneName, Description) VALUES
(1, 1, N'Khu rau củ', N'Tươi sống'),
(2, 1, N'Khu đồ uống', N'Nước giải khát'),
(3, 1, N'Khu bánh kẹo', N'Snack và bánh'),
(4, 1, N'Khu sữa', N'Sữa và chế phẩm từ sữa');
SET IDENTITY_INSERT dbo.ZONE OFF;
GO

SET IDENTITY_INSERT dbo.AISLE ON;
INSERT INTO dbo.AISLE (AisleID, ZoneID, AisleCode, AisleName) VALUES
(1, 1, N'A01', N'Dãy rau lá'),
(2, 1, N'A02', N'Dãy củ quả'),
(3, 2, N'B01', N'Dãy nước ngọt'),
(4, 2, N'B02', N'Dãy nước suối'),
(5, 3, N'C01', N'Dãy bánh ngọt'),
(6, 4, N'D01', N'Dãy sữa tươi');
SET IDENTITY_INSERT dbo.AISLE OFF;
GO

SET IDENTITY_INSERT dbo.SHELF ON;
INSERT INTO dbo.SHELF (ShelfID, AisleID, LevelNumber) VALUES
(1, 1, 1), (2, 1, 2),
(3, 2, 1), (4, 2, 2),
(5, 3, 1), (6, 3, 2),
(7, 4, 1),
(8, 5, 1), (9, 5, 2),
(10, 6, 1);
SET IDENTITY_INSERT dbo.SHELF OFF;
GO

SET IDENTITY_INSERT dbo.SLOT ON;
INSERT INTO dbo.SLOT (SlotID, ShelfID, SlotCode, Quantity, LastScannedAt) VALUES
(1, 1, N'S-A01-L1-01', 10, NULL),
(2, 1, N'S-A01-L1-02', 8, NULL),
(3, 2, N'S-A01-L2-01', 15, NULL),
(4, 3, N'S-A02-L1-01', 20, NULL),
(5, 3, N'S-A02-L1-02', 12, NULL),
(6, 5, N'S-B01-L1-01', 50, NULL),
(7, 5, N'S-B01-L1-02', 45, NULL),
(8, 6, N'S-B01-L2-01', 30, NULL),
(9, 7, N'S-B02-L1-01', 60, NULL),
(10, 8, N'S-C01-L1-01', 25, NULL),
(11, 9, N'S-C01-L2-01', 18, NULL),
(12, 10, N'S-D01-L1-01', 40, NULL);
SET IDENTITY_INSERT dbo.SLOT OFF;
GO

-- ═══════════════════════════════════════════════════════════
-- 4. CATEGORY + SUBCATEGORY + PRODUCT_TYPE
-- ═══════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.CATEGORY ON;
INSERT INTO dbo.CATEGORY (CategoryID, CategoryName) VALUES
(1, N'Thực phẩm tươi sống'),
(2, N'Đồ uống'),
(3, N'Bánh kẹo & Snack'),
(4, N'Sữa & Chế phẩm');
SET IDENTITY_INSERT dbo.CATEGORY OFF;
GO

SET IDENTITY_INSERT dbo.SUBCATEGORY ON;
INSERT INTO dbo.SUBCATEGORY (SubcategoryID, CategoryID, SubcategoryName) VALUES
(1, 1, N'Rau xanh'),
(2, 1, N'Củ quả'),
(3, 1, N'Trái cây'),
(4, 2, N'Nước ngọt'),
(5, 2, N'Nước suối'),
(6, 2, N'Nước ép'),
(7, 3, N'Bánh quy'),
(8, 3, N'Snack mặn'),
(9, 4, N'Sữa tươi'),
(10, 4, N'Sữa chua');
SET IDENTITY_INSERT dbo.SUBCATEGORY OFF;
GO

SET IDENTITY_INSERT dbo.PRODUCT_TYPE ON;
INSERT INTO dbo.PRODUCT_TYPE (ProductTypeID, SubcategoryID, TypeName) VALUES
(1, 1, N'Rau lá xanh'),
(2, 1, N'Rau ăn quả'),
(3, 2, N'Củ'),
(4, 2, N'Quả'),
(5, 3, N'Trái cây nhiệt đới'),
(6, 4, N'Nước có ga'),
(7, 4, N'Nước tăng lực'),
(8, 5, N'Nước khoáng'),
(9, 6, N'Nước ép trái cây'),
(10, 7, N'Bánh quy ngọt'),
(11, 8, N'Snack khoai tây'),
(12, 9, N'Sữa tươi không đường'),
(13, 9, N'Sữa tươi có đường'),
(14, 10, N'Sữa chua uống');
SET IDENTITY_INSERT dbo.PRODUCT_TYPE OFF;
GO

-- ═══════════════════════════════════════════════════════════
-- 5. PRODUCT (53 sản phẩm — BỎ cột AdCampaignID)
-- ═══════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.PRODUCT ON;
INSERT INTO dbo.PRODUCT (ProductID, ProductTypeID, ProductName, UnitPrice, PromotionPrice, ExpiredDate, ImageUrl, WeightOrVolume, Unit, Description, Status, SubstituteProductID) VALUES
-- Rau xanh (Type 1)
(1,  1, N'Rau muống', 8000.00, NULL, NULL, NULL, 1.000, N'kg', N'Rau muống tươi', N'Available', NULL),
(2,  1, N'Cải bó xôi', 12000.00, NULL, NULL, NULL, 0.500, N'kg', N'Cải bó xôi Đà Lạt', N'Available', NULL),
(3,  1, N'Xà lách xanh', 10000.00, NULL, NULL, NULL, 0.300, N'kg', N'Xà lách tươi', N'Available', NULL),
(4,  1, N'Rau cải thìa', 9000.00, NULL, NULL, NULL, 0.500, N'kg', N'Cải thìa tươi', N'Available', NULL),
(5,  1, N'Rau ngót', 7000.00, NULL, NULL, NULL, 0.300, N'kg', N'Rau ngót tươi', N'Available', NULL),
-- Rau ăn quả (Type 2)
(6,  2, N'Cà chua', 15000.00, 12000.00, NULL, NULL, 0.500, N'kg', N'Cà chua đỏ', N'Available', NULL),
(7,  2, N'Dưa chuột', 8000.00, NULL, NULL, NULL, 0.500, N'kg', N'Dưa chuột tươi', N'Available', NULL),
(8,  2, N'Ớt chuông', 25000.00, 20000.00, NULL, NULL, 0.300, N'kg', N'Ớt chuông đỏ', N'Available', NULL),
-- Củ (Type 3)
(9,  3, N'Khoai tây', 20000.00, NULL, NULL, NULL, 1.000, N'kg', N'Khoai tây Đà Lạt', N'Available', NULL),
(10, 3, N'Cà rốt', 18000.00, NULL, NULL, NULL, 1.000, N'kg', N'Cà rốt tươi', N'Available', NULL),
(11, 3, N'Củ hành tây', 22000.00, NULL, NULL, NULL, 0.500, N'kg', N'Hành tây', N'Available', NULL),
(12, 3, N'Tỏi', 45000.00, 40000.00, NULL, NULL, 0.200, N'kg', N'Tỏi ta', N'Available', NULL),
-- Trái cây nhiệt đới (Type 5)
(13, 5, N'Chuối tiêu', 25000.00, NULL, NULL, NULL, 1.000, N'kg', N'Chuối tiêu Nam Bộ', N'Available', NULL),
(14, 5, N'Xoài cát', 35000.00, 30000.00, NULL, NULL, 1.000, N'kg', N'Xoài cát Hòa Lộc', N'Available', NULL),
(15, 5, N'Ổi lê', 30000.00, NULL, NULL, NULL, 1.000, N'kg', N'Ổi lê Đài Loan', N'Available', NULL),
-- Nước có ga (Type 6)
(16, 6, N'Coca Cola 330ml', 10000.00, NULL, NULL, NULL, 330.000, N'ml', N'Nước ngọt có ga', N'Available', NULL),
(17, 6, N'Pepsi 330ml', 10000.00, NULL, NULL, NULL, 330.000, N'ml', N'Nước ngọt Pepsi', N'Available', NULL),
(18, 6, N'Sprite 330ml', 9000.00, NULL, NULL, NULL, 330.000, N'ml', N'Sprite chanh', N'Available', NULL),
(19, 6, N'Fanta Cam 330ml', 9000.00, NULL, NULL, NULL, 330.000, N'ml', N'Fanta vị cam', N'Available', NULL),
(20, 6, N'7UP 330ml', 9000.00, NULL, NULL, NULL, 330.000, N'ml', N'7UP chanh', N'Available', NULL),
-- Nước tăng lực (Type 7)
(21, 7, N'Red Bull 250ml', 12000.00, NULL, NULL, NULL, 250.000, N'ml', N'Nước tăng lực', N'Available', NULL),
(22, 7, N'Sting Dâu 330ml', 10000.00, NULL, NULL, NULL, 330.000, N'ml', N'Sting vị dâu', N'Available', NULL),
(23, 7, N'Number 1 330ml', 9000.00, NULL, NULL, NULL, 330.000, N'ml', N'Nước tăng lực Number 1', N'Available', NULL),
-- Nước khoáng (Type 8)
(24, 8, N'Aquafina 500ml', 7000.00, NULL, NULL, NULL, 500.000, N'ml', N'Nước suối tinh khiết', N'Available', NULL),
(25, 8, N'Dasani 500ml', 7000.00, NULL, NULL, NULL, 500.000, N'ml', N'Nước tinh khiết Dasani', N'Available', NULL),
(26, 8, N'La Vie 500ml', 8000.00, NULL, NULL, NULL, 500.000, N'ml', N'Nước khoáng La Vie', N'Available', NULL),
-- Nước ép (Type 9)
(27, 9, N'Tropicana Cam 330ml', 18000.00, 15000.00, NULL, NULL, 330.000, N'ml', N'Nước ép cam', N'Available', NULL),
(28, 9, N'Minute Maid Ổi 330ml', 16000.00, NULL, NULL, NULL, 330.000, N'ml', N'Nước ép ổi', N'Available', NULL),
-- Bánh quy ngọt (Type 10)
(29, 10, N'Oreo Kem Vani', 22000.00, NULL, NULL, NULL, 137.000, N'g', N'Bánh quy kẹp kem', N'Available', NULL),
(30, 10, N'Bánh Kinh Đô', 15000.00, 12000.00, NULL, NULL, 150.000, N'g', N'Bánh quy giòn', N'Available', NULL),
(31, 10, N'Marie Lu', 18000.00, NULL, NULL, NULL, 200.000, N'g', N'Bánh quy Marie', N'Available', NULL),
-- Snack khoai tây (Type 11)
(32, 11, N'Pringles Vị Cay', 45000.00, 40000.00, NULL, NULL, 165.000, N'g', N'Snack khoai tây ống', N'Available', NULL),
(33, 11, N'Lays Vị Phô Mai', 25000.00, NULL, NULL, NULL, 35.000, N'g', N'Snack khoai tây Lays', N'Available', NULL),
(34, 11, N'O'Star Vị BBQ', 20000.00, NULL, NULL, NULL, 42.000, N'g', N'Snack O Star', N'Available', NULL),
-- Sữa tươi không đường (Type 12)
(35, 12, N'Vinamilk Không Đường 180ml', 8000.00, NULL, NULL, NULL, 180.000, N'ml', N'Sữa tươi không đường', N'Available', NULL),
(36, 12, N'TH True Milk Không Đường 180ml', 9000.00, NULL, NULL, NULL, 180.000, N'ml', N'Sữa tươi sạch', N'Available', NULL),
(37, 12, N'Vinamilk Không Đường 1L', 38000.00, 35000.00, NULL, NULL, 1000.000, N'ml', N'Sữa tươi không đường 1L', N'Available', NULL),
-- Sữa tươi có đường (Type 13)
(38, 13, N'Vinamilk Có Đường 180ml', 8000.00, NULL, NULL, NULL, 180.000, N'ml', N'Sữa tươi có đường', N'Available', NULL),
(39, 13, N'Milo Sữa 180ml', 10000.00, NULL, NULL, NULL, 180.000, N'ml', N'Sữa Milo', N'Available', NULL),
(40, 13, N'Nestlé Milo 1L', 42000.00, 38000.00, NULL, NULL, 1000.000, N'ml', N'Sữa Milo 1L', N'Available', NULL),
-- Sữa chua uống (Type 14)
(41, 14, N'Vinamilk Probi 130ml', 8000.00, NULL, NULL, NULL, 130.000, N'ml', N'Sữa chua uống có men sống', N'Available', NULL),
(42, 14, N'TH True Yogurt Dâu 180ml', 12000.00, NULL, NULL, NULL, 180.000, N'ml', N'Sữa chua uống vị dâu', N'Available', NULL),
-- Thêm sản phẩm bổ sung để seed đủ
(43, 1, N'Rau mồng tơi', 7000.00, NULL, NULL, NULL, 0.300, N'kg', N'Rau mồng tơi tươi', N'Available', NULL),
(44, 1, N'Rau dền đỏ', 8000.00, NULL, NULL, NULL, 0.300, N'kg', N'Rau dền tươi', N'Available', NULL),
(45, 2, N'Mướp đắng', 12000.00, NULL, NULL, NULL, 0.500, N'kg', N'Khổ qua tươi', N'Available', NULL),
(46, 2, N'Bí đao', 10000.00, NULL, NULL, NULL, 1.000, N'kg', N'Bí đao xanh', N'Available', NULL),
(47, 3, N'Khoai lang tím', 25000.00, NULL, NULL, NULL, 1.000, N'kg', N'Khoai lang Nhật', N'Available', NULL),
(48, 3, N'Củ dền', 28000.00, NULL, NULL, NULL, 0.500, N'kg', N'Củ dền đỏ', N'Available', NULL),
(49, 5, N'Thanh long ruột đỏ', 30000.00, 25000.00, NULL, NULL, 1.000, N'kg', N'Thanh long Bình Thuận', N'Available', NULL),
(50, 5, N'Dưa hấu', 18000.00, NULL, NULL, NULL, 1.000, N'kg', N'Dưa hấu đỏ', N'Available', NULL),
(51, 6, N'Coca Cola 1.5L', 28000.00, 25000.00, NULL, NULL, 1500.000, N'ml', N'Coca Cola chai lớn', N'Available', NULL),
(52, 8, N'La Vie 1.5L', 18000.00, NULL, NULL, NULL, 1500.000, N'ml', N'Nước khoáng La Vie', N'Available', NULL),
(53, 10, N'Bánh Cosy Kem', 20000.00, NULL, NULL, NULL, 108.000, N'g', N'Bánh quy Cosy kẹp kem', N'Available', NULL);
SET IDENTITY_INSERT dbo.PRODUCT OFF;
GO

-- ═══════════════════════════════════════════════════════════
-- 6. PRODUCT_SLOT
-- ═══════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.PRODUCT_SLOT ON;
INSERT INTO dbo.PRODUCT_SLOT (ProductsSlotID, SlotID, ProductID) VALUES
(1, 1, 1), (2, 1, 2), (3, 2, 3), (4, 2, 4), (5, 3, 5),
(6, 4, 9), (7, 4, 10), (8, 5, 11), (9, 5, 12),
(10, 6, 16), (11, 6, 17), (12, 7, 18), (13, 7, 19), (14, 8, 20),
(15, 9, 24), (16, 9, 25), (17, 9, 26),
(18, 10, 29), (19, 10, 30), (20, 11, 31), (21, 11, 32),
(22, 12, 35), (23, 12, 36);
SET IDENTITY_INSERT dbo.PRODUCT_SLOT OFF;
GO

-- ═══════════════════════════════════════════════════════════
-- 7. HEALTH_TAG
-- ═══════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.HEALTH_TAG ON;
INSERT INTO dbo.HEALTH_TAG (HealthTagID, TagName, TagType) VALUES
(1, N'Đậu phộng', N'allergy'),
(2, N'Gluten', N'allergy'),
(3, N'Lactose', N'allergy'),
(4, N'Hải sản', N'allergy'),
(5, N'Thuần chay', N'diet'),
(6, N'Ít đường', N'diet'),
(7, N'Keto', N'diet'),
(8, N'Nhiều Vitamin C', N'nutrition'),
(9, N'Giàu chất xơ', N'nutrition'),
(10, N'Ít calo', N'nutrition'),
(11, N'Đậu nành', N'allergy'),
(12, N'Các loại hạt', N'allergy'),
(13, N'Cá', N'allergy'),
(14, N'Sữa', N'allergy'),
(15, N'Organic', N'diet'),
(16, N'Không chứa Gluten', N'diet'),
(17, N'Eat Clean', N'diet'),
(18, N'Địa Trung Hải', N'diet'),
(19, N'DASH', N'diet'),
(20, N'Ít béo', N'diet'),
(21, N'Ít calo', N'diet');
SET IDENTITY_INSERT dbo.HEALTH_TAG OFF;
GO

-- ═══════════════════════════════════════════════════════════
-- 8. MEMBERHEALTH_PREFERENCE
-- ═══════════════════════════════════════════════════════════
INSERT INTO dbo.MEMBERHEALTH_PREFERENCE (MemberID, HealthTagID, status) VALUES
(1, 1, N'Allergy'),
(1, 5, N'Prefer'),
(2, 2, N'Allergy'),
(2, 6, N'Prefer'),
(3, 3, N'Allergy'),
(4, 14, N'Allergy'),
(5, 11, N'Allergy'),
(6, 12, N'Allergy'),
(7, 4, N'Allergy'),
(8, 13, N'Allergy'),
(9, 14, N'Allergy'),
(10, 1, N'Allergy'),
(11, 2, N'Allergy'),
(12, 14, N'Allergy');
GO

-- ═══════════════════════════════════════════════════════════
-- 9. PRODUCT_HEALTHTAG
-- ═══════════════════════════════════════════════════════════
INSERT INTO dbo.PRODUCT_HEALTHTAG (ProductID, HealthTagID) VALUES
-- Mẫu ban đầu
(1, 5), (1, 9), (2, 5), (2, 8), (3, 5),
(4, 5), (5, 5), (6, 8), (13, 8), (14, 8),
(16, 6), (24, 6), (25, 6), (35, 6), (36, 6),
(29, 2), (30, 2), (31, 2), (32, 2),
-- Mì gói chứa Gluten (bột mì) và hải sản (Hảo Hảo tôm chua cay)
(4, 2), (4, 4), (5, 2),
-- Nhóm Sữa (Milk/Lactose)
(35, 14), (35, 3), (36, 14), (36, 3), (37, 14), (37, 3), (38, 14), (38, 3),
(39, 14), (39, 3), (40, 14), (40, 3), (41, 14), (41, 3), (42, 14), (42, 3),
-- Gạo hữu cơ/organic
(9, 15), (12, 15), (12, 5);
GO

-- ═══════════════════════════════════════════════════════════
-- 10. MEMBERSHIP
-- ═══════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.MEMBERSHIP ON;
INSERT INTO dbo.MEMBERSHIP (MembershipID, MemberID, TierName, Status) VALUES
(1, 1, N'Bronze', N'Active'),
(2, 2, N'Bronze', N'Active'),
(3, 3, N'Silver', N'Active');
SET IDENTITY_INSERT dbo.MEMBERSHIP OFF;
GO

-- ═══════════════════════════════════════════════════════════
-- 11. ROBOT
-- ═══════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.ROBOT ON;
INSERT INTO dbo.ROBOT (RobotID, RobotName, RobotCode, BatteryPct, Mode, Status, LastSeenAt) VALUES
(1, N'Robot 01', N'RB-001', 95, N'idle', N'Online', DATEADD(hour, 7, GETUTCDATE())),
(2, N'Robot 02', N'RB-002', 80, N'idle', N'Online', DATEADD(hour, 7, GETUTCDATE())),
(3, N'Robot 03', N'RB-003', 60, N'charging', N'Online', DATEADD(hour, 7, GETUTCDATE()));
SET IDENTITY_INSERT dbo.ROBOT OFF;
GO

-- ═══════════════════════════════════════════════════════════
-- 12. ROBOT_ZONE
-- ═══════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.ROBOT_ZONE ON;
INSERT INTO dbo.ROBOT_ZONE (RobotZoneID, RobotID, ZoneID) VALUES
(1, 1, 1),
(2, 1, 2),
(3, 2, 3),
(4, 3, 4);
SET IDENTITY_INSERT dbo.ROBOT_ZONE OFF;
GO

-- ═══════════════════════════════════════════════════════════
-- 13. BRAND
-- ═══════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.BRAND ON;
INSERT INTO dbo.BRAND (BrandID, BrandName, Wallet, Description, IsSystemBrand) VALUES
(1, N'Coca-Cola Company', 5000000.00, N'Hãng nước giải khát hàng đầu', 0),
(2, N'PepsiCo Vietnam', 3000000.00, N'Đồ uống giải khát', 0),
(3, N'Nestle Vietnam', 8000000.00, N'Sữa và thực phẩm dinh dưỡng', 0),
(4, N'Vinamilk', 10000000.00, N'Sữa tươi và sản phẩm từ sữa', 0),
(5, N'Unilever Vietnam', 6000000.00, N'Hàng tiêu dùng nhanh', 0),
(99, N'SmartMart', 0.00, N'Siêu thị SmartMart — Brand hệ thống tự chạy khuyến mãi', 1);
SET IDENTITY_INSERT dbo.BRAND OFF;
GO

-- ═══════════════════════════════════════════════════════════
-- 14. AD_PACKAGE
-- ═══════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.AD_PACKAGE ON;
INSERT INTO dbo.AD_PACKAGE (PackageID, PackageName, PricePackage, PriceRoute, PriceZone, PriceShelf, BasePriceClick, AdScore, Status) VALUES
(1, N'Gói Cơ Bản',      1000000.00, 200000.00,  30000.00, 15000.00,  5000.00,  50, N'Active'),
(2, N'Gói Tiêu Chuẩn', 3000000.00, 500000.00,  50000.00, 25000.00,  8000.00, 100, N'Active'),
(3, N'Gói Cao Cấp',    8000000.00, 1000000.00, 80000.00, 40000.00, 15000.00, 200, N'Active');
SET IDENTITY_INSERT dbo.AD_PACKAGE OFF;
GO

-- ═══════════════════════════════════════════════════════════
-- 15. AD_CAMPAIGN
-- ═══════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.AD_CAMPAIGN ON;
INSERT INTO dbo.AD_CAMPAIGN (AdCampaignID, PackageID, BrandID, SemanticObjectID, CampaignName, StartDate, EndDate, Status, ShelfPriceCharged, ShelfPurchasedAt) VALUES
(1, 1, 1, 1,    N'Coca mùa hè 2026',   DATEADD(hour, 7, GETUTCDATE()), DATEADD(month, 3, DATEADD(hour, 7, GETUTCDATE())), N'Active', 15000.00, DATEADD(hour, 7, GETUTCDATE())),
(2, 2, 4, 2,    N'Vinamilk tươi sạch', DATEADD(hour, 7, GETUTCDATE()), DATEADD(month, 6, DATEADD(hour, 7, GETUTCDATE())), N'Active', 25000.00, DATEADD(hour, 7, GETUTCDATE())),
(3, 3, 3, NULL, N'Nestle dinh dưỡng',  DATEADD(hour, 7, GETUTCDATE()), DATEADD(month, 2, DATEADD(hour, 7, GETUTCDATE())), N'Inactive', 0,       NULL);
SET IDENTITY_INSERT dbo.AD_CAMPAIGN OFF;
GO

-- 15a. AD_CAMPAIGN_ZONE — Mỗi Active campaign mua 2 Zone (PriceZone * 2).
-- Charge = PricePackage + (PriceZone × 2) cho mỗi impression tại các zone này.
INSERT INTO dbo.AD_CAMPAIGN_ZONE (AdCampaignID, ZoneID, ZonePriceCharged, PurchasedAt) VALUES
(1, 1, 30000.00, DATEADD(hour, 7, GETUTCDATE())),
(1, 2, 30000.00, DATEADD(hour, 7, GETUTCDATE())),
(2, 3, 50000.00, DATEADD(hour, 7, GETUTCDATE())),
(2, 4, 50000.00, DATEADD(hour, 7, GETUTCDATE()));
GO

-- 15b. AD_CAMPAIGN_ROUTE — Mỗi Active campaign mua 2 RobotRoute (PriceRoute * 2).
-- Charge = PricePackage + (PriceRoute × 2) cho mỗi impression trên các route này.
INSERT INTO dbo.AD_CAMPAIGN_ROUTE (AdCampaignID, RobotRouteID, RoutePriceCharged, PurchasedAt) VALUES
(1, 1, 200000.00, DATEADD(hour, 7, GETUTCDATE())),
(1, 2, 200000.00, DATEADD(hour, 7, GETUTCDATE())),
(2, 3, 500000.00, DATEADD(hour, 7, GETUTCDATE())),
(2, 4, 500000.00, DATEADD(hour, 7, GETUTCDATE()));
GO

-- ═══════════════════════════════════════════════════════════
-- 16. SPONSORED_PRODUCT
-- ═══════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.SPONSORED_PRODUCT ON;
INSERT INTO dbo.SPONSORED_PRODUCT (SponsoredID, AdCampaignID, ProductID, Priority, status) VALUES
(1, 1, 16, 10, N'Active'),
(2, 1, 51, 8, N'Active'),
(3, 2, 35, 10, N'Active'),
(4, 2, 37, 8, N'Active'),
(5, 3, 39, 5, N'Inactive');
SET IDENTITY_INSERT dbo.SPONSORED_PRODUCT OFF;
GO

-- ═══════════════════════════════════════════════════════════
-- 17. MEAL_SUGGESTION
-- ═══════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.MEAL_SUGGESTION ON;
INSERT INTO dbo.MEAL_SUGGESTION (MealSuggestionID, MealName, Description, YieldPortions, Calories, healthy_score, alternative_suggestion) VALUES
(1, N'Canh rau muống', N'Canh rau muống nấu tôm', 2, 120, 8.5, NULL),
(2, N'Salad rau trộn', N'Salad xà lách cà chua dưa chuột', 1, 90, 9.0, NULL),
(3, N'Sinh tố chuối sữa', N'Sinh tố chuối với sữa tươi', 1, 200, 7.5, NULL),
(4, N'Trái cây hỗn hợp', N'Xoài, chuối, thanh long', 2, 150, 9.5, NULL),
(5, N'Nước rau ép', N'Nước ép cà rốt cải xanh', 1, 60, 9.8, NULL),
(6, N'Khoai tây chiên', N'Khoai tây chiên giòn', 2, 350, 5.0, N'Khoai lang hấp'),
(7, N'Snack mix', N'Bánh quy và snack mix', 1, 280, 4.5, N'Trái cây tươi'),
(8, N'Sữa chua dâu', N'Sữa chua uống vị dâu', 1, 130, 8.0, NULL),
(9, N'Sinh tố xanh', N'Cải bó xôi, chuối, sữa', 1, 180, 9.2, NULL);
SET IDENTITY_INSERT dbo.MEAL_SUGGESTION OFF;
GO

-- ═══════════════════════════════════════════════════════════
-- 18. MEAL_ITEM (composite PK — không dùng IDENTITY_INSERT)
-- ═══════════════════════════════════════════════════════════
INSERT INTO dbo.MEAL_ITEM (MealSuggestionID, ProductID, QuantityRequired, UnitOfMeasure) VALUES
(1, 1, 0.300, N'kg'), (1, 10, 0.050, N'kg'),
(2, 3, 0.200, N'kg'), (2, 6, 0.200, N'kg'), (2, 7, 0.150, N'kg'),
(3, 13, 2.000, N'quả'), (3, 35, 1.000, N'hộp'),
(4, 13, 1.000, N'quả'), (4, 14, 0.300, N'kg'), (4, 49, 0.200, N'kg'),
(5, 2, 0.200, N'kg'), (5, 10, 0.100, N'kg'),
(6, 9, 0.500, N'kg'),
(7, 29, 1.000, N'gói'), (7, 33, 1.000, N'gói'),
(8, 42, 1.000, N'hộp'),
(9, 2, 0.200, N'kg'), (9, 13, 1.000, N'quả'), (9, 36, 1.000, N'hộp');
GO

-- ═══════════════════════════════════════════════════════════
-- 19. CART (1 cart per member)
-- ═══════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.CART ON;
INSERT INTO dbo.CART (CartID, MemberID, CreatedAt, UpdatedAt) VALUES
(1, 1, DATEADD(hour, 7, GETUTCDATE()), NULL),
(2, 2, DATEADD(hour, 7, GETUTCDATE()), NULL),
(3, 3, DATEADD(hour, 7, GETUTCDATE()), NULL);
SET IDENTITY_INSERT dbo.CART OFF;
GO

-- ═══════════════════════════════════════════════════════════
-- 20. CART_ITEM
-- ═══════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.CART_ITEM ON;
INSERT INTO dbo.CART_ITEM (CartItemID, CartID, ProductID, Quantity, AddedAt) VALUES
(1, 1, 1, 2, DATEADD(hour, 7, GETUTCDATE())),
(2, 1, 16, 3, DATEADD(hour, 7, GETUTCDATE())),
(3, 1, 29, 1, DATEADD(hour, 7, GETUTCDATE())),
(4, 2, 6, 1, DATEADD(hour, 7, GETUTCDATE())),
(5, 2, 35, 2, DATEADD(hour, 7, GETUTCDATE())),
(6, 3, 13, 3, DATEADD(hour, 7, GETUTCDATE())),
(7, 3, 24, 6, DATEADD(hour, 7, GETUTCDATE()));
SET IDENTITY_INSERT dbo.CART_ITEM OFF;
GO

-- ═══════════════════════════════════════════════════════════
-- 21. INVOICE_HISTORY
-- ═══════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.INVOICE_HISTORY ON;
INSERT INTO dbo.INVOICE_HISTORY (InvoiceHistoryID, MemberID, PurchaseDate, TotalPrice) VALUES
(1,  1, DATEADD(day, -30, DATEADD(hour, 7, GETUTCDATE())), 85000.00),
(2,  1, DATEADD(day, -25, DATEADD(hour, 7, GETUTCDATE())), 120000.00),
(3,  2, DATEADD(day, -20, DATEADD(hour, 7, GETUTCDATE())), 55000.00),
(4,  2, DATEADD(day, -15, DATEADD(hour, 7, GETUTCDATE())), 230000.00),
(5,  3, DATEADD(day, -10, DATEADD(hour, 7, GETUTCDATE())), 310000.00),
(6,  1, DATEADD(day, -7,  DATEADD(hour, 7, GETUTCDATE())), 95000.00),
(7,  3, DATEADD(day, -5,  DATEADD(hour, 7, GETUTCDATE())), 180000.00),
(8,  2, DATEADD(day, -3,  DATEADD(hour, 7, GETUTCDATE())), 145000.00),
(9,  1, DATEADD(day, -1,  DATEADD(hour, 7, GETUTCDATE())), 220000.00),
(10, 3, DATEADD(hour, 7, GETUTCDATE()), 76000.00);
SET IDENTITY_INSERT dbo.INVOICE_HISTORY OFF;
GO

-- ═══════════════════════════════════════════════════════════
-- 22. INVOICE_HISTORY_ITEM
-- ═══════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.INVOICE_HISTORY_ITEM ON;
INSERT INTO dbo.INVOICE_HISTORY_ITEM (InvoiceHistoryItemID, InvoiceHistoryID, ProductID, Quantity, UnitPrice) VALUES
(1,  1, 1,  2, 8000.00),
(2,  1, 16, 3, 10000.00),
(3,  1, 9,  1, 20000.00),
(4,  2, 35, 4, 8000.00),
(5,  2, 6,  2, 15000.00),
(6,  2, 29, 1, 22000.00),
(7,  3, 24, 3, 7000.00),
(8,  3, 13, 1, 25000.00),
(9,  4, 16, 6, 10000.00),
(10, 4, 35, 6, 8000.00),
(11, 4, 37, 2, 38000.00),
(12, 5, 32, 2, 45000.00),
(13, 5, 33, 3, 25000.00),
(14, 5, 39, 5, 10000.00),
(15, 5, 40, 2, 42000.00),
(16, 6, 1,  3, 8000.00),
(17, 6, 2,  2, 12000.00),
(18, 7, 14, 2, 35000.00),
(19, 7, 13, 3, 25000.00),
(20, 7, 50, 1, 18000.00),
(21, 8, 36, 3, 9000.00),
(22, 8, 38, 4, 8000.00),
(23, 9, 16, 6, 10000.00),
(24, 9, 51, 2, 28000.00),
(25, 9, 26, 4, 8000.00),
(26, 10, 1, 2, 8000.00),
(27, 10, 7, 1, 8000.00),
(28, 10, 24, 6, 7000.00);
SET IDENTITY_INSERT dbo.INVOICE_HISTORY_ITEM OFF;
GO

-- ═══════════════════════════════════════════════════════════
-- 23. AISLE_SCAN
-- ═══════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.AISLE_SCAN ON;
INSERT INTO dbo.AISLE_SCAN (ScanID, AisleID, AisleNodeID, RobotID, ScannedAt, EmptyPercentage, DensityPercentage, NeedsRestock, ImageUrl) VALUES
(1, 1, NULL, 1, DATEADD(hour, -2, DATEADD(hour, 7, GETUTCDATE())), 15.00, 85.00, 0, NULL),
(2, 2, NULL, 1, DATEADD(hour, -2, DATEADD(hour, 7, GETUTCDATE())), 20.00, 80.00, 0, NULL),
(3, 3, NULL, 2, DATEADD(hour, -1, DATEADD(hour, 7, GETUTCDATE())), 70.00, 30.00, 1, NULL),
(4, 4, NULL, 2, DATEADD(hour, -1, DATEADD(hour, 7, GETUTCDATE())), 5.00,  95.00, 0, NULL),
(5, 5, NULL, 1, DATEADD(hour, 7, GETUTCDATE()),                    40.00, 60.00, 0, NULL),
(6, 6, NULL, 3, DATEADD(hour, 7, GETUTCDATE()),                    10.00, 90.00, 0, NULL);
SET IDENTITY_INSERT dbo.AISLE_SCAN OFF;
GO

-- ═══════════════════════════════════════════════════════════
-- 24. MAP
-- ═══════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.MAP ON;
INSERT INTO dbo.MAP (MapID, FloorID, MapName, MapData, FloorplanImageUrl, CreatedAt) VALUES
(1, 1, N'Tầng 1 - Sơ đồ chính', NULL, NULL, DATEADD(hour, 7, GETUTCDATE()));
SET IDENTITY_INSERT dbo.MAP OFF;
GO

PRINT N'✅ seed_combined.sql: Hoàn thành! Đã insert đầy đủ data vào SuperMarketBot.';
GO
