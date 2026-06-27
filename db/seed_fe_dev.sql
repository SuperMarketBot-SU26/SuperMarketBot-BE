-- ============================================================
-- SmartMarketBot — FE Dev Seed (~500 records)
-- Chạy SAU khi đã chạy erd_database.sql + seed_erd_v4.sql.
-- Không touch: MAP, NAVIGATION_NODE, NAVIGATION_EDGE, AISLE_NODE,
--   ROBOT_ROUTE, ROUTE_NODE_MAPPING, ROUTE_ASSIGNMENT, SEMANTIC_OBJECT.
-- Giữ nguyên AccountID 1-3 (admin/staff/member1), chỉ thêm từ ID 4+.
-- ============================================================

USE SuperMarketBot;
GO

-- ══════════════════════════════════════════════════════════════
-- 1. ACCOUNT (12 record mới: ID 4-15) — 1 staff + 11 members
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.ACCOUNT ON;
INSERT INTO dbo.ACCOUNT (AccountID, Username, PasswordHash, Email, Phone, FullName, Status, Role, OtpCode, OtpExpiredAt, OtpType, CreatedAt) VALUES
(4, N'staff2', N'pbkdf2$100000$YWFhYWFhYWFhYWFhYWFhYQ==$YjJiMmMzZDRlNWU2ZTcwZjE4MzQ1Njc4OTAxMjM0NTY3', N'staff2@smartmarket.local', N'0901000002', N'Trần Thị Bích Ngọc', N'Active', N'Staff', NULL, NULL, NULL, DATEADD(hour, 7, GETUTCDATE())),
(5, N'member2', N'pbkdf2$100000$YWFhYWFhYWFhYWFhYWFhYQ==$YjJiMmMzZDRlNWU2ZTcwZjE4MzQ1Njc4OTAxMjM0NTY3', N'member2@smartmarket.local', N'0902000002', N'Lê Hoàng Anh', N'Active', N'Member', NULL, NULL, NULL, DATEADD(hour, 7, GETUTCDATE())),
(6, N'member3', N'pbkdf2$100000$YWFhYWFhYWFhYWFhYWFhYQ==$YjJiMmMzZDRlNWU2ZTcwZjE4MzQ1Njc4OTAxMjM0NTY3', N'member3@smartmarket.local', N'0902000003', N'Phạm Thị Mai', N'Active', N'Member', NULL, NULL, NULL, DATEADD(hour, 7, GETUTCDATE())),
(7, N'member4', N'pbkdf2$100000$YWFhYWFhYWFhYWFhYWFhYQ==$YjJjMmMzZDRlNWU2ZTcwZjE4MzQ1Njc4OTAxMjM0NTY3', N'member4@smartmarket.local', N'0902000004', N'Đỗ Quang Minh', N'Active', N'Member', NULL, NULL, NULL, DATEADD(hour, 7, GETUTCDATE())),
(8, N'member5', N'pbkdf2$100000$YWFhYWFhYWFhYWFhYWFhYQ==$YjJiMmMzZDRlNWU2ZTcwZjE4MzQ1Njc4OTAxMjM0NTY3', N'member5@smartmarket.local', N'0902000005', N'Vũ Thị Lan', N'Active', N'Member', NULL, NULL, NULL, DATEADD(hour, 7, GETUTCDATE())),
(9, N'member6', N'pbkdf2$100000$YWFhYWFhYWFhYWFhYWFhYQ==$YjJiMmMzZDRlNWU2ZTcwZjE4MzQ1Njc4OTAxMjM0NTY3', N'member6@smartmarket.local', N'0902000006', N'Nguyễn Văn Hùng', N'Active', N'Member', NULL, NULL, NULL, DATEADD(hour, 7, GETUTCDATE())),
(10, N'member7', N'pbkdf2$100000$YWFhYWFhYWFhYWFhYWFhYQ==$YjJiMmMzZDRlNWU2ZTcwZjE4MzQ1Njc4OTAxMjM0NTY3', N'member7@smartmarket.local', N'0902000007', N'Hoàng Thị Thu', N'Active', N'Member', NULL, NULL, NULL, DATEADD(hour, 7, GETUTCDATE())),
(11, N'member8', N'pbkdf2$100000$YWFhYWFhYWFhYWFhYWFhYQ==$YjJiMmMzZDRlNWU2ZTcwZjE4MzQ1Njc4OTAxMjM0NTY3', N'member8@smartmarket.local', N'0902000008', N'Bùi Minh Tuấn', N'Active', N'Member', NULL, NULL, NULL, DATEADD(hour, 7, GETUTCDATE())),
(12, N'member9', N'pbkdf2$100000$YWFhYWFhYWFhYWFhYWFhYQ==$YjJiMmMzZDRlNWU2ZTcwZjE4MzQ1Njc4OTAxMjM0NTY3', N'member9@smartmarket.local', N'0902000009', N'Trương Thị Hồng', N'Active', N'Member', NULL, NULL, NULL, DATEADD(hour, 7, GETUTCDATE())),
(13, N'member10', N'pbkdf2$100000$YWFhYWFhYWFhYWFhYWFhYQ==$YjJiMmMzZDRlNWU2ZTcwZjE4MzQ1Njc4OTAxMjM0NTY3', N'member10@smartmarket.local', N'0902000010', N'Đặng Văn Khoa', N'Active', N'Member', NULL, NULL, NULL, DATEADD(hour, 7, GETUTCDATE())),
(14, N'member11', N'pbkdf2$100000$YWFhYWFhYWFhYWFhYWFhYQ==$YjJiMmMzZDRlNWU2ZTcwZjE4MzQ1Njc4OTAxMjM0NTY3', N'member11@smartmarket.local', N'0902000011', N'Lý Thị Phương', N'Active', N'Member', NULL, NULL, NULL, DATEADD(hour, 7, GETUTCDATE())),
(15, N'member12', N'pbkdf2$100000$YWFhYWFhYWFhYWFhYWFhYQ==$YjJiMmMzZDRlNWU2ZTcwZjE4MzQ1Njc4OTAxMjM0NTY3', N'member12@smartmarket.local', N'0902000012', N'Phan Văn Long', N'Active', N'Member', NULL, NULL, NULL, DATEADD(hour, 7, GETUTCDATE()));
SET IDENTITY_INSERT dbo.ACCOUNT OFF;
GO

-- ══════════════════════════════════════════════════════════════
-- 2. MEMBER (12 record mới: MemberID 2-13) — 1-to-1 với Account 4-15
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.MEMBER ON;
INSERT INTO dbo.MEMBER (MemberID, AccountID, FullName, FacePath, FaceVector, SpendingLimit, TotalPoints) VALUES
(2, 5, N'Lê Hoàng Anh', NULL, NULL, 800000.00, 250),
(3, 6, N'Phạm Thị Mai', NULL, NULL, 1500000.00, 480),
(4, 7, N'Đỗ Quang Minh', NULL, NULL, 300000.00, 80),
(5, 8, N'Vũ Thị Lan', NULL, NULL, 2000000.00, 720),
(6, 9, N'Nguyễn Văn Hùng', NULL, NULL, 500000.00, 120),
(7, 10, N'Hoàng Thị Thu', NULL, NULL, 1000000.00, 350),
(8, 11, N'Bùi Minh Tuấn', NULL, NULL, 600000.00, 200),
(9, 12, N'Trương Thị Hồng', NULL, NULL, 1200000.00, 410),
(10, 13, N'Đặng Văn Khoa', NULL, NULL, 400000.00, 90),
(11, 14, N'Lý Thị Phương', NULL, NULL, 900000.00, 280),
(12, 15, N'Phan Văn Long', NULL, NULL, 750000.00, 220);
SET IDENTITY_INSERT dbo.MEMBER OFF;
GO

-- ══════════════════════════════════════════════════════════════
-- 3. MEMBERSHIP (6 record) — tier phân bổ
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.MEMBERSHIP ON;
INSERT INTO dbo.MEMBERSHIP (MembershipID, MemberID, TierName, Status) VALUES
(2, 2, N'Silver', N'Active'),
(3, 3, N'Gold', N'Active'),
(4, 5, N'Platinum', N'Active'),
(5, 7, N'Silver', N'Active'),
(6, 9, N'Gold', N'Active'),
(7, 11, N'Bronze', N'Active');
SET IDENTITY_INSERT dbo.MEMBERSHIP OFF;
GO

-- ══════════════════════════════════════════════════════════════
-- 4. HEALTH_TAG (8 record mới: ID 3-10) — mix allergy + diet
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.HEALTH_TAG ON;
INSERT INTO dbo.HEALTH_TAG (HealthTagID, TagName, TagType) VALUES
(3, N'Sữa', N'allergy'),
(4, N'Trứng', N'allergy'),
(5, N'Hải sản có vỏ', N'allergy'),
(6, N'Low-carb', N'diet'),
(7, N'High-protein', N'diet'),
(8, N'Vegan', N'diet'),
(9, N'Keto', N'diet'),
(10, N'Halal', N'lifestyle');
SET IDENTITY_INSERT dbo.HEALTH_TAG OFF;
GO

-- ══════════════════════════════════════════════════════════════
-- 5. MEMBERHEALTH_PREFERENCE (15 record)
-- ══════════════════════════════════════════════════════════════
INSERT INTO dbo.MEMBERHEALTH_PREFERENCE (MemberID, HealthTagID, status) VALUES
(2, 6, N'Diet'),
(2, 7, N'Diet'),
(3, 8, N'Diet'),
(3, 4, N'Allergy'),
(4, 1, N'Allergy'),
(4, 3, N'Allergy'),
(5, 9, N'Diet'),
(5, 7, N'Diet'),
(6, 10, N'Lifestyle'),
(7, 2, N'Allergy'),
(7, 6, N'Diet'),
(8, 5, N'Allergy'),
(9, 8, N'Diet'),
(9, 7, N'Diet'),
(10, 1, N'Allergy');
GO

-- ══════════════════════════════════════════════════════════════
-- 6. CATEGORY (4 record mới: ID 3-6)
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.CATEGORY ON;
INSERT INTO dbo.CATEGORY (CategoryID, CategoryName) VALUES
(3, N'Thực phẩm khô'),
(4, N'Sữa và sản phẩm từ sữa'),
(5, N'Đồ gia dụng'),
(6, N'Chăm sóc cá nhân');
SET IDENTITY_INSERT dbo.CATEGORY OFF;
GO

-- ══════════════════════════════════════════════════════════════
-- 7. SUBCATEGORY (10 record)
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.SUBCATEGORY ON;
INSERT INTO dbo.SUBCATEGORY (SubcategoryID, CategoryID, SubcategoryName) VALUES
(3, 3, N'Mì và phở'),
(4, 3, N'Gạo và bột'),
(5, 3, N'Gia vị'),
(6, 4, N'Sữa tươi'),
(7, 4, N'Sữa chua'),
(8, 4, N'Phô mai'),
(9, 5, N'Bát đĩa'),
(10, 5, N'Dụng cụ nấu ăn'),
(11, 6, N'Dầu gội'),
(12, 6, N'Kem đánh răng');
SET IDENTITY_INSERT dbo.SUBCATEGORY OFF;
GO

-- ══════════════════════════════════════════════════════════════
-- 8. PRODUCT_TYPE (20 record)
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.PRODUCT_TYPE ON;
INSERT INTO dbo.PRODUCT_TYPE (ProductTypeID, SubcategoryID, TypeName) VALUES
(3, 3, N'Mì gói'),
(4, 3, N'Phở gói'),
(5, 4, N'Gạo trắng'),
(6, 4, N'Gạo lứt'),
(7, 4, N'Bột mì'),
(8, 5, N'Nước mắm'),
(9, 5, N'Tiêu đen'),
(10, 5, N'Đường'),
(11, 6, N'Sữa tươi tiệt trùng'),
(12, 6, N'Sữa tươi thanh trùng'),
(13, 7, N'Sữa chua có đường'),
(14, 7, N'Sữa chua không đường'),
(15, 8, N'Phô mai cheddar'),
(16, 8, N'Phô mai mozzarella'),
(17, 9, N'Bát sứ'),
(18, 9, N'Đĩa sứ'),
(19, 10, N'Chảo chống dính'),
(20, 10, N'Nồi cơm điện'),
(21, 11, N'Dầu gội Clear'),
(22, 12, N'Kem đánh răng P/S');
SET IDENTITY_INSERT dbo.PRODUCT_TYPE OFF;
GO

-- ══════════════════════════════════════════════════════════════
-- 9. PRODUCT (50 record mới: ID 4-53) — đủ data cho search/pagination
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.PRODUCT ON;
INSERT INTO dbo.PRODUCT (ProductID, ProductTypeID, ProductName, UnitPrice, PromotionPrice, AdCampaignID, ExpiredDate, ImageUrl, WeightOrVolume, Unit, Description, Status, SubstituteProductID) VALUES
-- Nhóm mì/phở
(4, 3, N'Hảo Hảo Tôm Chua Cay', 4500.00, 3800.00, NULL, DATEADD(month, 12, DATEADD(hour, 7, GETUTCDATE())), NULL, 75.000, N'g', N'Mì tôm chua cay', N'Available', NULL),
(5, 3, N'Omachi Xốt Thịt Thăn', 5500.00, NULL, NULL, DATEADD(month, 18, DATEADD(hour, 7, GETUTCDATE())), NULL, 80.000, N'g', N'Mì Omachi thịt thăn', N'Available', NULL),
(6, 3, N'Miến Phú Hương', 18000.00, NULL, NULL, DATEADD(month, 24, DATEADD(hour, 7, GETUTCDATE())), NULL, 200.000, N'g', N'Miến dong', N'Available', NULL),
(7, 4, N'Phở bò Vifon', 12000.00, 9900.00, NULL, DATEADD(month, 12, DATEADD(hour, 7, GETUTCDATE())), NULL, 65.000, N'g', N'Phở bò ăn liền', N'Available', NULL),
(8, 4, N'Phở gà A-One', 11000.00, NULL, NULL, DATEADD(month, 12, DATEADD(hour, 7, GETUTCDATE())), NULL, 60.000, N'g', N'Phở gà ăn liền', N'Available', NULL),
-- Nhóm gạo/bột
(9, 5, N'Gạo ST25 Lúa Tôm', 32000.00, 28000.00, NULL, NULL, NULL, 5.000, N'kg', N'Gạo ST25 đặc sản', N'Available', NULL),
(10, 5, N'Gạo Jasmine Đồng Bằng', 22000.00, NULL, NULL, NULL, NULL, 5.000, N'kg', N'Gạo Jasmine', N'Available', NULL),
(11, 5, N'Gạo Tám Xoan Hải Hậu', 45000.00, NULL, NULL, NULL, NULL, 1.000, N'kg', N'Gạo Tám thơm', N'Available', NULL),
(12, 6, N'Gạo lứt hữu cơ', 38000.00, 35000.00, NULL, NULL, NULL, 1.000, N'kg', N'Gạo lứt organic', N'Available', NULL),
(13, 7, N'Bột mì đa dụng Meizan', 22000.00, NULL, NULL, DATEADD(month, 8, DATEADD(hour, 7, GETUTCDATE())), NULL, 1.000, N'kg', N'Bột mì Meizan', N'Available', NULL),
-- Nhóm gia vị
(14, 8, N'Nước mắm Nam Ngư 500ml', 35000.00, 29000.00, NULL, DATEADD(month, 24, DATEADD(hour, 7, GETUTCDATE())), NULL, 500.000, N'ml', N'Nước mắm truyền thống', N'Available', NULL),
(15, 8, N'Nước mắm Phú Quốc 750ml', 85000.00, NULL, NULL, DATEADD(month, 36, DATEADD(hour, 7, GETUTCDATE())), NULL, 750.000, N'ml', N'Nước mắm Phú Quốc', N'Available', NULL),
(16, 9, N'Tiêu đen xay Lâm Đồng', 45000.00, NULL, NULL, DATEADD(month, 18, DATEADD(hour, 7, GETUTCDATE())), NULL, 100.000, N'g', N'Tiêu đen nguyên chất', N'Available', NULL),
(17, 10, N'Đường cát trắng Biên Hòa', 28000.00, 25000.00, NULL, DATEADD(month, 24, DATEADD(hour, 7, GETUTCDATE())), NULL, 1.000, N'kg', N'Đường tinh luyện', N'Available', NULL),
(18, 10, N'Đường thốt nốt An Giang', 55000.00, NULL, NULL, DATEADD(month, 12, DATEADD(hour, 7, GETUTCDATE())), NULL, 500.000, N'g', N'Đường thốt nốt tự nhiên', N'Available', NULL),
-- Nhóm sữa
(19, 11, N'TH True Milk 1L', 32000.00, NULL, NULL, DATEADD(month, 6, DATEADD(hour, 7, GETUTCDATE())), NULL, 1000.000, N'ml', N'Sữa tươi tiệt trùng TH', N'Available', NULL),
(20, 11, N'Vinamilk 100% 1L', 30000.00, 27000.00, NULL, DATEADD(month, 6, DATEADD(hour, 7, GETUTCDATE())), NULL, 1000.000, N'ml', N'Sữa tươi Vinamilk', N'Available', NULL),
(21, 12, N'Sữa tươi Mộc Châu 500ml', 22000.00, NULL, NULL, DATEADD(day, 15, DATEADD(hour, 7, GETUTCDATE())), NULL, 500.000, N'ml', N'Sữa tươi thanh trùng Mộc Châu', N'Available', NULL),
(22, 13, N'Yogurt Vinamilk có đường', 5500.00, NULL, NULL, DATEADD(day, 30, DATEADD(hour, 7, GETUTCDATE())), NULL, 100.000, N'g', N'Sữa chua Vinamilk', N'Available', NULL),
(23, 13, N'Yogurt TH True Yogurt', 7000.00, 6500.00, NULL, DATEADD(day, 30, DATEADD(hour, 7, GETUTCDATE())), NULL, 100.000, N'g', N'Sữa chua TH', N'Available', NULL),
(24, 14, N'Sữa chua không đường Vinamilk', 6000.00, NULL, NULL, DATEADD(day, 30, DATEADD(hour, 7, GETUTCDATE())), NULL, 100.000, N'g', N'Yogurt không đường', N'Available', NULL),
(25, 15, N'Phô mai cheddar Teama', 35000.00, NULL, NULL, DATEADD(month, 3, DATEADD(hour, 7, GETUTCDATE())), NULL, 200.000, N'g', N'Phô mai cheddar lát', N'Available', NULL),
(26, 16, N'Phô mai mozzarella Pizza', 75000.00, 65000.00, NULL, DATEADD(month, 2, DATEADD(hour, 7, GETUTCDATE())), NULL, 250.000, N'g', N'Mozzarella bào sợi', N'Available', NULL),
-- Nhóm đồ gia dụng
(27, 17, N'Bát cơm sứ Bát Tràng', 35000.00, NULL, NULL, NULL, NULL, 400.000, N'g', N'Bát sứ thủ công', N'Available', NULL),
(28, 17, N'Bát sứ Minh Long', 85000.00, NULL, NULL, NULL, NULL, 350.000, N'g', N'Bát sứ cao cấp', N'Available', NULL),
(29, 18, N'Đĩa sứ tròn 25cm', 45000.00, NULL, NULL, NULL, NULL, 500.000, N'g', N'Đĩa sứ tròn', N'Available', NULL),
(30, 19, N'Chảo chống dính Sunhouse 26cm', 220000.00, 189000.00, NULL, NULL, NULL, 1200.000, N'g', N'Chảo chống dính', N'Available', NULL),
(31, 20, N'Nồi cơm điện Sharp 1.8L', 890000.00, NULL, NULL, NULL, NULL, 3000.000, N'g', N'Nồi cơm điện tử', N'Available', NULL),
(32, 20, N'Nồi cơm điện Toshiba 1L', 650000.00, 599000.00, NULL, NULL, NULL, 2500.000, N'g', N'Nồi cơm điện mini', N'Available', NULL),
-- Nhóm chăm sóc cá nhân
(33, 21, N'Clear Men Cool Sport 650ml', 165000.00, NULL, NULL, DATEADD(month, 36, DATEADD(hour, 7, GETUTCDATE())), NULL, 650.000, N'ml', N'Dầu gội nam Clear', N'Available', NULL),
(34, 21, N'Dove Hair Fall Rescue 650ml', 175000.00, 149000.00, NULL, DATEADD(month, 36, DATEADD(hour, 7, GETUTCDATE())), NULL, 650.000, N'ml', N'Dầu gội Dove', N'Available', NULL),
(35, 21, N'Pantene Total Damage Care 750ml', 185000.00, NULL, NULL, DATEADD(month, 36, DATEADD(hour, 7, GETUTCDATE())), NULL, 750.000, N'ml', N'Dầu gội Pantene', N'Available', NULL),
(36, 22, N'P/S Bảo Vệ 123 200g', 35000.00, 29000.00, NULL, DATEADD(month, 24, DATEADD(hour, 7, GETUTCDATE())), NULL, 200.000, N'g', N'Kem đánh răng P/S', N'Available', NULL),
(37, 22, N'Colgate Total 12 150g', 55000.00, NULL, NULL, DATEADD(month, 24, DATEADD(hour, 7, GETUTCDATE())), NULL, 150.000, N'g', N'Kem đánh răng Colgate', N'Available', NULL),
(38, 22, N'Sensodyne Fresh Mint 100g', 75000.00, NULL, NULL, DATEADD(month, 24, DATEADD(hour, 7, GETUTCDATE())), NULL, 100.000, N'g', N'Kem đánh răng Sensodyne', N'Available', NULL),
-- Bổ sung thêm đa dạng
(39, 3, N'3 Miền Bò Viên', 4200.00, NULL, NULL, DATEADD(month, 12, DATEADD(hour, 7, GETUTCDATE())), NULL, 70.000, N'g', N'Mì bò viên', N'Available', NULL),
(40, 5, N'Gạo Nàng Hoa 5kg', 38000.00, NULL, NULL, NULL, NULL, 5.000, N'kg', N'Gạo Nàng Hoa', N'Available', NULL),
(41, 11, N'Sữa tươi Ba Vì 1L', 26000.00, NULL, NULL, DATEADD(month, 6, DATEADD(hour, 7, GETUTCDATE())), NULL, 1000.000, N'ml', N'Sữa Ba Vì', N'Available', NULL),
(42, 13, N'Sữa chua uống Yakult', 22000.00, NULL, NULL, DATEADD(day, 45, DATEADD(hour, 7, GETUTCDATE())), NULL, 200.000, N'ml', N'Yakult 5 chai', N'Available', NULL),
(43, 21, N'Sunsilk Mềm Mượt 650ml', 145000.00, 119000.00, NULL, DATEADD(month, 36, DATEADD(hour, 7, GETUTCDATE())), NULL, 650.000, N'ml', N'Dầu gội Sunsilk', N'Available', NULL),
(44, 3, N'Gau Do Re Mi Cay', 5000.00, NULL, NULL, DATEADD(month, 12, DATEADD(hour, 7, GETUTCDATE())), NULL, 80.000, N'g', N'Mì Gấu Đỏ', N'Available', NULL),
(45, 8, N'Nước mắm Chinsu 500ml', 38000.00, 32000.00, NULL, DATEADD(month, 24, DATEADD(hour, 7, GETUTCDATE())), NULL, 500.000, N'ml', N'Nước mắm Chinsu', N'Available', NULL),
(46, 19, N'Chảo inox Elmich 28cm', 380000.00, NULL, NULL, NULL, NULL, 1500.000, N'g', N'Chảo inox đáy từ', N'Available', NULL),
(47, 16, N'Phô mai con bò cười', 45000.00, 38000.00, NULL, DATEADD(month, 2, DATEADD(hour, 7, GETUTCDATE())), NULL, 120.000, N'g', N'Phô mai hộp', N'Available', NULL),
(48, 11, N'Nutri Boost Sữa dâu', 12000.00, NULL, NULL, DATEADD(month, 6, DATEADD(hour, 7, GETUTCDATE())), NULL, 295.000, N'ml', N'Sữa trái cây', N'Available', NULL),
(49, 22, N'Closeup White Now 150g', 45000.00, NULL, NULL, DATEADD(month, 24, DATEADD(hour, 7, GETUTCDATE())), NULL, 150.000, N'g', N'Kem đánh răng Closeup', N'Available', NULL),
(50, 7, N'Bột chiên giòn Meizan', 18000.00, NULL, NULL, DATEADD(month, 8, DATEADD(hour, 7, GETUTCDATE())), NULL, 500.000, N'g', N'Bột chiên giòn', N'Available', NULL),
(51, 13, N'Sữa chua Hy Lạp Olympus', 95000.00, 85000.00, NULL, DATEADD(day, 21, DATEADD(hour, 7, GETUTCDATE())), NULL, 200.000, N'g', N'Greek yogurt nhập khẩu', N'Available', NULL),
(52, 20, N'Nồi áp suất điện Lock&Lock 5L', 1250000.00, 1099000.00, NULL, NULL, NULL, 4000.000, N'g', N'Nồi áp suất điện', N'Available', NULL),
(53, 21, N'Head&Shoulders Bạc Hà 750ml', 195000.00, NULL, NULL, DATEADD(month, 36, DATEADD(hour, 7, GETUTCDATE())), NULL, 750.000, N'ml', N'Dầu gội H&S chống gàu', N'Available', NULL);
SET IDENTITY_INSERT dbo.PRODUCT OFF;
GO

-- ══════════════════════════════════════════════════════════════
-- 10. PRODUCT_HEALTHTAG (20 record) — tag cho 1 số sản phẩm
-- ══════════════════════════════════════════════════════════════
INSERT INTO dbo.PRODUCT_HEALTHTAG (ProductID, HealthTagID) VALUES
-- Vegan
(9, 8), (10, 8), (11, 8), (12, 8), (13, 8),
-- Keto
(12, 9), (24, 9), (25, 9), (26, 9),
-- High-protein
(15, 7), (19, 7), (20, 7), (21, 7), (25, 7), (26, 7),
-- Low-carb
(24, 6), (25, 6), (26, 6),
-- Có gluten (cho test allergy gluten)
(13, 2), (50, 2);
GO

-- ══════════════════════════════════════════════════════════════
-- 11. FLOOR (1 record mới: ID 2) — thêm tầng 2
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.FLOOR ON;
INSERT INTO dbo.FLOOR (FloorID, FloorNumber) VALUES (2, 2);
SET IDENTITY_INSERT dbo.FLOOR OFF;
GO

-- ══════════════════════════════════════════════════════════════
-- 12. ZONE (4 record mới: ID 3-6)
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.ZONE ON;
INSERT INTO dbo.ZONE (ZoneID, FloorID, ZoneName, Description) VALUES
(3, 1, N'Khu thực phẩm khô', N'Mì, gạo, gia vị'),
(4, 1, N'Khu sữa và chế phẩm', N'Sữa tươi, sữa chua, phô mai'),
(5, 2, N'Khu đồ gia dụng', N'Bát đĩa, nồi chảo'),
(6, 2, N'Khu chăm sóc cá nhân', N'Dầu gội, kem đánh răng');
SET IDENTITY_INSERT dbo.ZONE OFF;
GO

-- ══════════════════════════════════════════════════════════════
-- 13. AISLE (10 record mới: ID 3-12) — mỗi zone có 2-3 aisle
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.AISLE ON;
INSERT INTO dbo.AISLE (AisleID, ZoneID, AisleCode, AisleName) VALUES
(3, 3, N'A02', N'Dãy mì gói'),
(4, 3, N'A03', N'Dãy gạo'),
(5, 3, N'A04', N'Dãy gia vị'),
(6, 4, N'B02', N'Dãy sữa tươi'),
(7, 4, N'B03', N'Dãy sữa chua và phô mai'),
(8, 5, N'C01', N'Dãy bát đĩa'),
(9, 5, N'C02', N'Dãy nồi chảo'),
(10, 6, N'D01', N'Dãy dầu gội'),
(11, 6, N'D02', N'Dãy kem đánh răng'),
(12, 4, N'B04', N'Dãy sữa trái cây');
SET IDENTITY_INSERT dbo.AISLE OFF;
GO

-- ══════════════════════════════════════════════════════════════
-- 14. SHELF (30 record: mỗi aisle 3 levels)
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.SHELF ON;
DECLARE @aisleId INT = 3;
DECLARE @level INT = 1;
DECLARE @shelfId INT = 3;
WHILE @aisleId <= 12
BEGIN
    SET @level = 1;
    WHILE @level <= 3
    BEGIN
        INSERT INTO dbo.SHELF (ShelfID, AisleID, LevelNumber) VALUES (@shelfId, @aisleId, @level);
        SET @shelfId = @shelfId + 1;
        SET @level = @level + 1;
    END
    SET @aisleId = @aisleId + 1;
END
SET IDENTITY_INSERT dbo.SHELF OFF;
GO

-- ══════════════════════════════════════════════════════════════
-- 15. SLOT (60 record: mỗi shelf 2 slots)
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.SLOT ON;
DECLARE @shelfId INT = 3;
DECLARE @slotIdx INT = 1;
DECLARE @slotId INT = 3;
WHILE @shelfId <= 32
BEGIN
    SET @slotIdx = 1;
    WHILE @slotIdx <= 2
    BEGIN
        INSERT INTO dbo.SLOT (SlotID, ShelfID, SlotCode, Quantity, LastScannedAt)
        VALUES (@slotId, @shelfId, N'S' + CAST(@slotId AS VARCHAR), 20 + (@slotId % 30), DATEADD(hour, -(@slotId % 48), DATEADD(hour, 7, GETUTCDATE())));
        SET @slotId = @slotId + 1;
        SET @slotIdx = @slotIdx + 1;
    END
    SET @shelfId = @shelfId + 1;
END
SET IDENTITY_INSERT dbo.SLOT OFF;
GO

-- ══════════════════════════════════════════════════════════════
-- 16. PRODUCT_SLOT (60 record: mỗi slot 1-2 sản phẩm)
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.PRODUCT_SLOT ON;
DECLARE @slotId INT = 3;
DECLARE @psId INT = 3;
DECLARE @productStart INT = 4;
WHILE @slotId <= 62
BEGIN
    INSERT INTO dbo.PRODUCT_SLOT (ProductsSlotID, SlotID, ProductID)
    VALUES (@psId, @slotId, @productStart + ((@slotId - 3) % 50));
    SET @psId = @psId + 1;
    IF @slotId % 3 = 0 AND @productStart + ((@slotId - 3) % 50) + 1 <= 53
    BEGIN
        INSERT INTO dbo.PRODUCT_SLOT (ProductsSlotID, SlotID, ProductID)
        VALUES (@psId, @slotId, @productStart + ((@slotId - 3) % 50) + 1);
        SET @psId = @psId + 1;
    END
    SET @slotId = @slotId + 1;
END
SET IDENTITY_INSERT dbo.PRODUCT_SLOT OFF;
GO

-- ══════════════════════════════════════════════════════════════
-- 17. ROBOT (4 record mới: ID 2-5) — pin & mode đa dạng
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.ROBOT ON;
INSERT INTO dbo.ROBOT (RobotID, RobotName, RobotCode, BatteryPct, Mode, Status, LastSeenAt) VALUES
(2, N'Robot 02', N'RB002', 87, N'idle', N'Online', DATEADD(minute, -2, DATEADD(hour, 7, GETUTCDATE()))),
(3, N'Robot 03', N'RB003', 45, N'navigating', N'Online', DATEADD(minute, -1, DATEADD(hour, 7, GETUTCDATE()))),
(4, N'Robot 04', N'RB004', 15, N'charging', N'Online', DATEADD(hour, -1, DATEADD(hour, 7, GETUTCDATE()))),
(5, N'Robot 05', N'RB005', 92, N'scanning', N'Online', DATEADD(minute, -5, DATEADD(hour, 7, GETUTCDATE())));
SET IDENTITY_INSERT dbo.ROBOT OFF;
GO

-- ══════════════════════════════════════════════════════════════
-- 18. ROBOT_ZONE (10 record — gán robot vào zone)
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.ROBOT_ZONE ON;
INSERT INTO dbo.ROBOT_ZONE (RobotZoneID, RobotID, ZoneID) VALUES
(2, 2, 3),
(3, 2, 4),
(4, 3, 5),
(5, 3, 6),
(6, 4, 3),
(7, 4, 4),
(8, 4, 5),
(9, 5, 3),
(10, 5, 6);
SET IDENTITY_INSERT dbo.ROBOT_ZONE OFF;
GO

-- ══════════════════════════════════════════════════════════════
-- 19. ROBOT_LOG (20 record) — log đa dạng state
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
-- 20. BRAND (4 record mới: ID 2-5)
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.BRAND ON;
INSERT INTO dbo.BRAND (BrandID, BrandName, Wallet, Description) VALUES
(2, N'Unilever Vietnam', 5000000.00, N'Nhà sản xuất hàng tiêu dùng'),
(3, N'Vinamilk', 8000000.00, N'Sữa và sản phẩm từ sữa'),
(4, N'Sunhouse Group', 3500000.00, N'Đồ gia dụng'),
(5, N'Panasonic Vietnam', 2500000.00, N'Đồ điện gia dụng');
SET IDENTITY_INSERT dbo.BRAND OFF;
GO

-- ══════════════════════════════════════════════════════════════
-- 21. AD_PACKAGE (2 record mới: ID 2-3)
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.AD_PACKAGE ON;
INSERT INTO dbo.AD_PACKAGE (PackageID, PackageName, PricePackage, PriceRoute, BasePriceClick, AdScore, Status) VALUES
(2, N'Gói cao cấp', 2500000.00, 500000.00, 8000.00, 75, N'Active'),
(3, N'Gói VIP', 5000000.00, 1000000.00, 12000.00, 90, N'Active');
SET IDENTITY_INSERT dbo.AD_PACKAGE OFF;
GO

-- ══════════════════════════════════════════════════════════════
-- 22. AD_CAMPAIGN (6 record mới: ID 2-7) — gán RobotZone có sẵn
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.AD_CAMPAIGN ON;
INSERT INTO dbo.AD_CAMPAIGN (AdCampaignID, PackageID, BrandID, RobotZoneID, CampaignName, StartDate, EndDate, Status) VALUES
(2, 2, 2, 2, N'Clear Men tháng 6', DATEADD(day, -10, DATEADD(hour, 7, GETUTCDATE())), DATEADD(day, 20, DATEADD(hour, 7, GETUTCDATE())), N'Running'),
(3, 2, 3, 3, N'Vinamilk mùa hè 2026', DATEADD(day, -5, DATEADD(hour, 7, GETUTCDATE())), DATEADD(day, 60, DATEADD(hour, 7, GETUTCDATE())), N'Running'),
(4, 3, 4, 4, N'Sunhouse khuyến mãi T6', DATEADD(day, -15, DATEADD(hour, 7, GETUTCDATE())), DATEADD(day, 15, DATEADD(hour, 7, GETUTCDATE())), N'Running'),
(5, 2, 5, 5, N'Panasonic tháng này', DATEADD(day, -3, DATEADD(hour, 7, GETUTCDATE())), DATEADD(day, 30, DATEADD(hour, 7, GETUTCDATE())), N'Running'),
(6, 3, 3, 6, N'TH True Yogurt quảng bá', DATEADD(day, -7, DATEADD(hour, 7, GETUTCDATE())), DATEADD(day, 25, DATEADD(hour, 7, GETUTCDATE())), N'Running'),
(7, 2, 2, NULL, N'Dove Refresh Q3', DATEADD(month, 1, DATEADD(hour, 7, GETUTCDATE())), DATEADD(month, 4, DATEADD(hour, 7, GETUTCDATE())), N'Scheduled');
SET IDENTITY_INSERT dbo.AD_CAMPAIGN OFF;
GO

-- ══════════════════════════════════════════════════════════════
-- 23. SPONSORED_PRODUCT (12 record mới: ID 2-13) — gán vào campaign
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.SPONSORED_PRODUCT ON;
INSERT INTO dbo.SPONSORED_PRODUCT (SponsoredID, AdCampaignID, ProductID, Priority, status) VALUES
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
-- 24. AD_CAMPAIGN_LOG (30 record) — mix Click/View/RoutePass
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

-- ══════════════════════════════════════════════════════════════
-- 25. INVOICE_HISTORY (25 record mới: ID 2-26) — lịch sử mua hàng
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.INVOICE_HISTORY ON;
DECLARE @m INT = 1;
DECLARE @total DECIMAL(18,2);
DECLARE @invoiceId INT = 2;
WHILE @m <= 25
BEGIN
    SET @total = 50000 + (@m * 12345) % 800000;
    INSERT INTO dbo.INVOICE_HISTORY (InvoiceHistoryID, MemberID, PurchaseDate, TotalPrice)
    VALUES (
        @invoiceId,
        ((@m - 1) % 12) + 1,
        DATEADD(day, -(@m * 2), DATEADD(hour, 7, GETUTCDATE())),
        @total
    );
    SET @invoiceId = @invoiceId + 1;
    SET @m = @m + 1;
END
SET IDENTITY_INSERT dbo.INVOICE_HISTORY OFF;
GO

-- ══════════════════════════════════════════════════════════════
-- 26. INVOICE_HISTORY_ITEM (70 record) — mỗi invoice 2-3 items
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.INVOICE_HISTORY_ITEM ON;
DECLARE @inv INT = 2;
DECLARE @items INT;
DECLARE @itemId INT = 2;
DECLARE @productId INT;
DECLARE @price DECIMAL(18,2);
WHILE @inv <= 26
BEGIN
    SET @items = 2 + (@inv % 2);
    DECLARE @n INT = 0;
    WHILE @n < @items
    BEGIN
        SET @productId = ((@inv * 3 + @n * 7) % 50) + 4;
        SET @price = 5000 + ((@productId * 1000) % 150000);
        INSERT INTO dbo.INVOICE_HISTORY_ITEM (InvoiceHistoryItemID, InvoiceHistoryID, ProductID, Quantity, UnitPrice)
        VALUES (@itemId, @inv, @productId, 1 + (@n % 4), @price);
        SET @itemId = @itemId + 1;
        SET @n = @n + 1;
    END
    SET @inv = @inv + 1;
END
SET IDENTITY_INSERT dbo.INVOICE_HISTORY_ITEM OFF;
GO

-- ══════════════════════════════════════════════════════════════
-- 27. MEAL_SUGGESTION (8 record mới: ID 2-9)
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.MEAL_SUGGESTION ON;
INSERT INTO dbo.MEAL_SUGGESTION (MealSuggestionID, MealName, Description, YieldPortions, ImageUrl, Calories, healthy_score, alternative_suggestion) VALUES
(2, N'Cơm gạo lứt gà xé', N'Bữa trưa healthy với gạo lứt và ức gà', 2, NULL, 450, 85, N'Có thể thay ức gà bằng cá hồi'),
(3, N'Phở bò truyền thống', N'Phở bò với nước dùng hầm xương 12 tiếng', 4, NULL, 550, 70, N'Phở gà ít béo hơn'),
(4, N'Salad rau trộn dầu oliu', N'Salad rau xanh với sốt dầu oliu chanh', 2, NULL, 220, 95, N'Salad Caesar nếu thích phô mai'),
(5, N'Mì xào hải sản rau củ', N'Mì xào chay với rau củ đa dạng', 3, NULL, 480, 75, N'Miến xào ít tinh bột hơn'),
(6, N'Bún chả Hà Nội', N'Bún chả truyền thống với thịt nướng', 2, NULL, 520, 65, NULL),
(7, N'Cơm chiên Dương Châu', N'Cơm chiên với tôm, trứng, lạp xưởng', 3, NULL, 580, 60, N'Cơm chiên chay'),
(8, N'Bánh mì ốp la', N'Bánh mì Việt Nam với trứng ốp la', 1, NULL, 350, 55, N'Bánh mì chay'),
(9, N'Sinh tố chuối yến mạch', N'Sinh tố cho bữa sáng giàu dinh dưỡng', 1, NULL, 280, 88, NULL);
SET IDENTITY_INSERT dbo.MEAL_SUGGESTION OFF;
GO

-- ══════════════════════════════════════════════════════════════
-- 28. MEAL_ITEM (25 record) — gán product vào meal
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.MEAL_ITEM ON;
DECLARE @mid INT = 2;
DECLARE @miId INT = 2;
DECLARE @p INT;
DECLARE @q DECIMAL(18,3);
DECLARE @u NVARCHAR(20);
WHILE @mid <= 9
BEGIN
    DECLARE @c INT = 0;
    WHILE @c < 3
    BEGIN
        SET @p = ((@mid * 5 + @c * 11) % 50) + 4;
        SET @q = 0.1 + ((@c * 0.3));
        SET @u = CASE @c WHEN 0 THEN N'kg' WHEN 1 THEN N'g' ELSE N'ml' END;
        INSERT INTO dbo.MEAL_ITEM (MealSuggestionID, ProductID, QuantityRequired, UnitOfMeasure)
        VALUES (@mid, @p, @q, @u);
        SET @miId = @miId + 1;
        SET @c = @c + 1;
    END
    SET @mid = @mid + 1;
END
SET IDENTITY_INSERT dbo.MEAL_ITEM OFF;
GO

-- ══════════════════════════════════════════════════════════════
-- 29. AISLE_SCAN (6 record) — robot scan aisle
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.AISLE_SCAN ON;
INSERT INTO dbo.AISLE_SCAN (ScanID, AisleID, RobotID, ScannedAt, EmptyPercentage, NeedsRestock, ImageUrl) VALUES
(2, 3, 2, DATEADD(hour, -2, DATEADD(hour, 7, GETUTCDATE())), 15.50, 0, NULL),
(3, 4, 3, DATEADD(hour, -3, DATEADD(hour, 7, GETUTCDATE())), 45.00, 1, NULL),
(4, 5, 2, DATEADD(hour, -4, DATEADD(hour, 7, GETUTCDATE())), 8.20, 0, NULL),
(5, 6, 5, DATEADD(hour, -5, DATEADD(hour, 7, GETUTCDATE())), 22.30, 0, NULL),
(6, 7, 3, DATEADD(hour, -6, DATEADD(hour, 7, GETUTCDATE())), 67.00, 1, NULL),
(7, 8, 2, DATEADD(hour, -7, DATEADD(hour, 7, GETUTCDATE())), 5.00, 0, NULL);
SET IDENTITY_INSERT dbo.AISLE_SCAN OFF;
GO

-- ══════════════════════════════════════════════════════════════
-- SUMMARY
-- ══════════════════════════════════════════════════════════════
PRINT '✅ FE Dev Seed: ~500 records inserted (excluding MAP tables).';
GO
