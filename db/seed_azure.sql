-- ============================================================================
-- SMARTMARKETBOT — AZURE SEED DATA v2 (Dynamic FK lookup, safe to re-run)
-- ============================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

-- Reset identity nếu bảng còn trống (tránh gap từ lần chạy lỗi trước)
IF NOT EXISTS (SELECT 1 FROM dbo.Accounts) DBCC CHECKIDENT ('Accounts', RESEED, 0);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.Members) DBCC CHECKIDENT ('Members', RESEED, 0);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.Admins)  DBCC CHECKIDENT ('Admins', RESEED, 0);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.Staff)   DBCC CHECKIDENT ('Staff', RESEED, 0);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.Floors)  DBCC CHECKIDENT ('Floors', RESEED, 0);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.Zones)   DBCC CHECKIDENT ('Zones', RESEED, 0);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.Aisles)  DBCC CHECKIDENT ('Aisles', RESEED, 0);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.ShelfLevels)     DBCC CHECKIDENT ('ShelfLevels', RESEED, 0);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.Categories)      DBCC CHECKIDENT ('Categories', RESEED, 0);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.Subcategories)   DBCC CHECKIDENT ('Subcategories', RESEED, 0);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.ProductTypes)    DBCC CHECKIDENT ('ProductTypes', RESEED, 0);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.Products)        DBCC CHECKIDENT ('Products', RESEED, 0);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.Robots)          DBCC CHECKIDENT ('Robots', RESEED, 0);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.Maps)            DBCC CHECKIDENT ('Maps', RESEED, 0);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.NavigationNodes) DBCC CHECKIDENT ('NavigationNodes', RESEED, 0);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.NavigationEdges) DBCC CHECKIDENT ('NavigationEdges', RESEED, 0);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.HealthTags)      DBCC CHECKIDENT ('HealthTags', RESEED, 0);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.Brands)          DBCC CHECKIDENT ('Brands', RESEED, 0);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.AdPackages)      DBCC CHECKIDENT ('AdPackages', RESEED, 0);
GO

-- ============================================================================
-- PART 1: SQL VIEWS
-- ============================================================================
DROP VIEW IF EXISTS dbo.PurchaseHistory;
GO
CREATE VIEW dbo.PurchaseHistory AS
SELECT hi.HistoryItemID AS PurchaseID, sh.MemberID, p.ProductName, sh.ShoppingDate AS PurchaseDate
FROM dbo.HistoryItems hi
INNER JOIN dbo.ShoppingHistories sh ON hi.ShoppingHistoryID = sh.ShoppingHistoryID
INNER JOIN dbo.Products p ON hi.ProductID = p.ProductID;
GO

DROP VIEW IF EXISTS dbo.Store_Map;
GO
CREATE VIEW dbo.Store_Map AS
SELECT p.ProductID AS MapID, p.ProductName,
    ISNULL(N'Kệ ' + a.AisleCode, N'Chưa xếp kệ') AS ShelfLocation,
    ISNULL(z.ZoneName, N'Khu vực chung') AS Landmark,
    ISNULL(a.AisleName, N'') AS AisleNote
FROM dbo.Products p
LEFT JOIN dbo.Slots sl    ON p.ProductID = sl.ProductID
LEFT JOIN dbo.ShelfLevels slv ON sl.ShelfLevelID = slv.ShelfLevelID
LEFT JOIN dbo.Aisles a    ON slv.AisleID = a.AisleID
LEFT JOIN dbo.Zones z     ON a.ZoneID = z.ZoneID;
GO

DROP VIEW IF EXISTS dbo.Blocked_Aisles;
GO
CREATE VIEW dbo.Blocked_Aisles AS
SELECT AisleID, AisleCode, IsBlocked, BlockReason AS Reason FROM dbo.Aisles;
GO

DROP VIEW IF EXISTS dbo.Real_Time_Stock;
GO
CREATE VIEW dbo.Real_Time_Stock AS
SELECT p.ProductID AS StockID, p.ProductName,
    ISNULL((SELECT SUM(Quantity) FROM dbo.Slots WHERE ProductID = p.ProductID), 0) AS StockQuantity,
    sub.ProductName AS SubstituteProduct
FROM dbo.Products p
LEFT JOIN dbo.Products sub ON p.SubstituteProductID = sub.ProductID;
GO

-- ============================================================================
-- PART 2: SEED DATA
-- ============================================================================

-- 1. Accounts
IF NOT EXISTS (SELECT 1 FROM dbo.Accounts WHERE Username = 'admin_lth')
INSERT INTO dbo.Accounts (Username, PasswordHash, Email, Phone, IsActive, EmailConfirmed, FullName, Role, CreatedAt) VALUES
('admin_lth',    'hash_pbkdf2_code_123', 'hieultse161727@fpt.edu.vn', '0986515253', 1, 1, N'Lê Tiến Hiếu',    1, GETUTCDATE()),
('member_qhuy',  'hash_pbkdf2_code_123', 'huynqse160498@fpt.edu.vn',  '0782766322', 1, 1, N'Nguyễn Quang Huy', 3, GETUTCDATE()),
('member_ahung', 'hash_pbkdf2_code_123', 'hungnase180159@fpt.edu.vn', '0868205403', 1, 1, N'Nguyễn Anh Hùng',  3, GETUTCDATE()),
('staff_dtnhan', 'hash_pbkdf2_code_123', 'nhandt35@fe.edu.vn',        '0903056041', 1, 1, N'Đỗ Tấn Nhàn',     2, GETUTCDATE());
GO

-- 2. Members (lookup AccountID by Username)
IF NOT EXISTS (SELECT 1 FROM dbo.Members WHERE PhoneNumber = '0782766322')
INSERT INTO dbo.Members (AccountID, FullName, PhoneNumber, FacePath, FaceVector, Tier, TotalPoints, SearchMode, ShoppingBudget)
SELECT AccountID, N'Nguyễn Quang Huy', '0782766322', N'/storage/faces/huy_nq.jpg', N'[0.015,-0.042,0.125,-0.098]', N'Gold', 1500, 'Healthy', 200000.00
FROM dbo.Accounts WHERE Username = 'member_qhuy';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Members WHERE PhoneNumber = '0868205403')
INSERT INTO dbo.Members (AccountID, FullName, PhoneNumber, FacePath, FaceVector, Tier, TotalPoints, SearchMode, ShoppingBudget)
SELECT AccountID, N'Nguyễn Anh Hùng', '0868205403', N'/storage/faces/hung_na.jpg', N'[-0.032,0.088,0.054,0.112]', N'Silver', 800, 'Budget', 150000.00
FROM dbo.Accounts WHERE Username = 'member_ahung';
GO

-- 3. Admins (lookup by Username)
IF NOT EXISTS (SELECT 1 FROM dbo.Admins)
INSERT INTO dbo.Admins (AccountID)
SELECT AccountID FROM dbo.Accounts WHERE Username = 'admin_lth';
GO

-- 4. Staff (lookup by Username)
IF NOT EXISTS (SELECT 1 FROM dbo.Staff)
INSERT INTO dbo.Staff (AccountID, FirstName, LastName, Phone, Email)
SELECT AccountID, N'Nhàn', N'Đỗ Tấn', '0903056041', 'nhandt35@fe.edu.vn'
FROM dbo.Accounts WHERE Username = 'staff_dtnhan';
GO

-- 5. Floors
IF NOT EXISTS (SELECT 1 FROM dbo.Floors)
INSERT INTO dbo.Floors (FloorNumber) VALUES (1);
GO

-- 6. Zones
IF NOT EXISTS (SELECT 1 FROM dbo.Zones)
INSERT INTO dbo.Zones (FloorID, ZoneCode, ZoneName, Description, IsBlocked)
SELECT FloorID,'A',N'Thực phẩm tươi sống',N'Khu bán hoa quả, rau củ, thịt cá tươi sống',0 FROM dbo.Floors WHERE FloorNumber=1
UNION ALL
SELECT FloorID,'B',N'Đồ uống & Giải khát',N'Khu nước ngọt, bia, sữa tươi và trà giải nhiệt',0 FROM dbo.Floors WHERE FloorNumber=1
UNION ALL
SELECT FloorID,'C',N'Hóa mỹ phẩm & Đồ dùng',N'Khu dầu gội, nước rửa chén, chất tẩy rửa',0 FROM dbo.Floors WHERE FloorNumber=1
UNION ALL
SELECT FloorID,'D',N'Bánh kẹo & Đồ ăn vặt',N'Khu bánh ngọt, snack, kẹo dẻo cho trẻ em',0 FROM dbo.Floors WHERE FloorNumber=1
UNION ALL
SELECT FloorID,'E',N'Gia vị & Đồ khô',N'Khu hạt nêm, nước mắm, mì tôm ăn liền',0 FROM dbo.Floors WHERE FloorNumber=1;
GO

-- 7. Aisles (lookup ZoneID by ZoneCode)
IF NOT EXISTS (SELECT 1 FROM dbo.Aisles)
BEGIN
    DECLARE @zA INT, @zB INT, @zC INT, @zD INT, @zE INT;
    SELECT @zA=ZoneID FROM dbo.Zones WHERE ZoneCode='A';
    SELECT @zB=ZoneID FROM dbo.Zones WHERE ZoneCode='B';
    SELECT @zC=ZoneID FROM dbo.Zones WHERE ZoneCode='C';
    SELECT @zD=ZoneID FROM dbo.Zones WHERE ZoneCode='D';
    SELECT @zE=ZoneID FROM dbo.Zones WHERE ZoneCode='E';

    INSERT INTO dbo.Aisles (ZoneID,AisleCode,AisleName,IsBlocked,BlockReason) VALUES 
    (@zA,'A1',N'Dãy trái cây nhập khẩu',        0,NULL),
    (@zA,'A2',N'Dãy rau xanh hữu cơ',           0,NULL),
    (@zB,'B1',N'Kệ nước giải khát & Nước ngọt', 0,NULL),
    (@zB,'B2',N'Kệ sữa tươi & Sữa chua',        0,NULL),
    (@zC,'C1',N'Kệ hóa phẩm vệ sinh gia đình',  0,NULL),
    (@zC,'C2',N'Kệ dầu gội & Sữa tắm',          0,NULL),
    (@zD,'D1',N'Dãy bánh quy & Bánh xốp',        0,NULL),
    (@zD,'D2',N'Dãy khoai tây chiên & Snack',   0,NULL),
    (@zE,'E1',N'Kệ mì ăn liền & Đồ ăn khô',    0,NULL),
    (@zE,'E2',N'Kệ gia vị mắm muối bột ngọt',   0,NULL);
END
GO

-- 8. ShelfLevels (3 tầng mỗi dãy, lookup AisleID by AisleCode)
IF NOT EXISTS (SELECT 1 FROM dbo.ShelfLevels)
BEGIN
    INSERT INTO dbo.ShelfLevels (AisleID, LevelNumber)
    SELECT a.AisleID, n.n FROM dbo.Aisles a
    CROSS JOIN (SELECT 1 n UNION ALL SELECT 2 UNION ALL SELECT 3) n;
END
GO

-- 9. HealthTags
IF NOT EXISTS (SELECT 1 FROM dbo.HealthTags)
INSERT INTO dbo.HealthTags (TagName, TagType) VALUES 
(N'Không đường',       'diet'),
(N'Ít béo',            'diet'),
(N'Thuần chay (Vegan)','diet'),
(N'Organic Hữu cơ',    'diet'),
(N'Dị ứng sữa',        'allergy'),
(N'Dị ứng hạt lạc',    'allergy'),
(N'Dị ứng hải sản',    'allergy');
GO

-- 10. MemberHealthPreferences (lookup MemberID + TagID)
IF NOT EXISTS (SELECT 1 FROM dbo.MemberHealthPreferences)
BEGIN
    INSERT INTO dbo.MemberHealthPreferences (MemberID, TagID, IsAllergy)
    SELECT m.MemberID, t.TagID, 0 FROM dbo.Members m, dbo.HealthTags t
    WHERE m.PhoneNumber='0782766322' AND t.TagName=N'Không đường'; -- Huy thích không đường
    
    INSERT INTO dbo.MemberHealthPreferences (MemberID, TagID, IsAllergy)
    SELECT m.MemberID, t.TagID, 0 FROM dbo.Members m, dbo.HealthTags t
    WHERE m.PhoneNumber='0782766322' AND t.TagName=N'Organic Hữu cơ'; -- Huy thích Organic
    
    INSERT INTO dbo.MemberHealthPreferences (MemberID, TagID, IsAllergy)
    SELECT m.MemberID, t.TagID, 1 FROM dbo.Members m, dbo.HealthTags t
    WHERE m.PhoneNumber='0868205403' AND t.TagName=N'Dị ứng hạt lạc'; -- Hùng dị ứng lạc
END
GO

-- 11. Categories
IF NOT EXISTS (SELECT 1 FROM dbo.Categories)
INSERT INTO dbo.Categories (CategoryName, Description) VALUES
(N'Hàng Tiêu Dùng Nhanh (FMCG)', N'Sản phẩm thiết yếu ăn uống hàng ngày'),
(N'Hóa Mỹ Phẩm & Chăm Sóc',      N'Vật dụng tẩy rửa gia đình và vệ sinh cá nhân');
GO

-- 12. Subcategories (lookup CategoryID)
IF NOT EXISTS (SELECT 1 FROM dbo.Subcategories)
BEGIN
    DECLARE @cFMCG INT, @cHmp INT;
    SELECT @cFMCG=CategoryID FROM dbo.Categories WHERE CategoryName LIKE N'Hàng Tiêu%';
    SELECT @cHmp =CategoryID FROM dbo.Categories WHERE CategoryName LIKE N'Hóa Mỹ%';
    INSERT INTO dbo.Subcategories (CategoryID, SubcategoryName) VALUES
    (@cFMCG, N'Nước Giải Khát & Đồ Uống'),
    (@cFMCG, N'Sữa & Sản phẩm từ Sữa'),
    (@cFMCG, N'Mì Ăn Liền & Đồ Khô'),
    (@cFMCG, N'Bánh Kẹo & Đồ Ăn Vặt'),
    (@cHmp,  N'Hóa Phẩm Gia Đình');
END
GO

-- 13. ProductTypes (lookup SubcategoryID)
IF NOT EXISTS (SELECT 1 FROM dbo.ProductTypes)
BEGIN
    DECLARE @sNuoc INT,@sSua INT,@sMi INT,@sBanh INT,@sHoa INT;
    SELECT @sNuoc=SubcategoryID FROM dbo.Subcategories WHERE SubcategoryName LIKE N'Nước Giải%';
    SELECT @sSua =SubcategoryID FROM dbo.Subcategories WHERE SubcategoryName LIKE N'Sữa%';
    SELECT @sMi  =SubcategoryID FROM dbo.Subcategories WHERE SubcategoryName LIKE N'Mì%';
    SELECT @sBanh=SubcategoryID FROM dbo.Subcategories WHERE SubcategoryName LIKE N'Bánh%';
    SELECT @sHoa =SubcategoryID FROM dbo.Subcategories WHERE SubcategoryName LIKE N'Hóa Phẩm%';
    INSERT INTO dbo.ProductTypes (SubcategoryID, ProductTypeName) VALUES
    (@sNuoc, N'Nước ngọt có ga'),
    (@sNuoc, N'Trà & Nước trái cây đóng chai'),
    (@sSua,  N'Sữa tươi tiệt trùng'),
    (@sSua,  N'Sữa chua ăn'),
    (@sMi,   N'Mì tôm gói'),
    (@sMi,   N'Đồ khô đóng gói'),
    (@sBanh, N'Bánh ngọt công nghiệp'),
    (@sBanh, N'Snack khoai tây chiên'),
    (@sHoa,  N'Nước rửa chén vệ sinh');
END
GO

-- 14. Products (lookup ProductTypeID)
IF NOT EXISTS (SELECT 1 FROM dbo.Products)
BEGIN
    DECLARE @ptNgot INT,@ptTra INT,@ptSuaTuoi INT,@ptSuaChua INT,
            @ptMi INT,@ptKho INT,@ptBanh INT,@ptSnack INT,@ptHoa INT;
    SELECT @ptNgot   =ProductTypeID FROM dbo.ProductTypes WHERE ProductTypeName=N'Nước ngọt có ga';
    SELECT @ptTra    =ProductTypeID FROM dbo.ProductTypes WHERE ProductTypeName LIKE N'Trà%';
    SELECT @ptSuaTuoi=ProductTypeID FROM dbo.ProductTypes WHERE ProductTypeName=N'Sữa tươi tiệt trùng';
    SELECT @ptSuaChua=ProductTypeID FROM dbo.ProductTypes WHERE ProductTypeName=N'Sữa chua ăn';
    SELECT @ptMi     =ProductTypeID FROM dbo.ProductTypes WHERE ProductTypeName=N'Mì tôm gói';
    SELECT @ptKho    =ProductTypeID FROM dbo.ProductTypes WHERE ProductTypeName=N'Đồ khô đóng gói';
    SELECT @ptBanh   =ProductTypeID FROM dbo.ProductTypes WHERE ProductTypeName=N'Bánh ngọt công nghiệp';
    SELECT @ptSnack  =ProductTypeID FROM dbo.ProductTypes WHERE ProductTypeName=N'Snack khoai tây chiên';
    SELECT @ptHoa    =ProductTypeID FROM dbo.ProductTypes WHERE ProductTypeName=N'Nước rửa chén vệ sinh';

    INSERT INTO dbo.Products (ProductTypeID,ProductName,UnitPrice,Barcode,ImageUrl,WeightOrVolume,Unit,Description,IsActive) VALUES
    (@ptNgot,   N'Nước ngọt Coca Cola lon',           10000,'8935049500015',N'/images/products/coca_cola.jpg',    320,'ml',N'Nước ngọt giải khát có ga truyền thống',1),
    (@ptNgot,   N'Nước ngọt Pepsi lon',                9500,'8935049500022',N'/images/products/pepsi.jpg',        320,'ml',N'Nước giải khát có ga mát lạnh sảng khoái',1),
    (@ptNgot,   N'Nước ngọt Coca Cola Không Đường',   10500,'8935049500039',N'/images/products/coca_zero.jpg',   320,'ml',N'Phiên bản không đường tốt cho sức khỏe',1),
    (@ptTra,    N'Trà xanh không độ chai',             12000,'8935049500046',N'/images/products/tra_xanh_0d.jpg', 455,'ml',N'Trà xanh chiết xuất từ lá trà tươi mát',1),
    (@ptSuaTuoi,N'Sữa tươi tiệt trùng TH True Milk',  38000,'8935049500053',N'/images/products/th_it_duong.jpg',1000,'ml',N'Sữa tươi tiệt trùng từ trang trại TH',1),
    (@ptSuaTuoi,N'Sữa tươi tiệt trùng Vinamilk',      36000,'8935049500060',N'/images/products/vnm_khong_duong.jpg',1000,'ml',N'Sữa tươi nguyên chất không thêm đường',1),
    (@ptSuaChua,N'Sữa chua ăn Vinamilk hộp',           8000,'8935049500077',N'/images/products/vnm_yogurt.jpg',  100,'g', N'Sữa chua bổ sung men vi sinh',1),
    (@ptMi,     N'Mì tôm Hảo Hảo tôm chua cay',        4500,'8935049500084',N'/images/products/hao_hao.jpg',      75,'g', N'Mì ăn liền quốc dân hương vị tôm chua cay',1),
    (@ptMi,     N'Mì trộn Omachi xốt sườn hầm',        8500,'8935049500091',N'/images/products/omachi_suon.jpg',  80,'g', N'Mì khoai tây xốt sườn hầm thơm phức',1),
    (@ptKho,    N'Gạo ST25 Ông Cua túi cao cấp',      185000,'8935049500107',N'/images/products/gao_st25.jpg',     5,'kg',N'Gạo ngon nhất thế giới, hạt dẻo hương dứa',1),
    (@ptBanh,   N'Bánh ChocoPie Orion hộp lớn',        55000,'8935049500114',N'/images/products/chocopie.jpg',   396,'g', N'Bánh socola nhân marshmallow hấp dẫn',1),
    (@ptBanh,   N'Bánh trứng Custas Orion hộp',        48000,'8935049500121',N'/images/products/custas.jpg',     141,'g', N'Bánh bông lan nhân kem trứng thơm ngon',1),
    (@ptSnack,  N'Snack khoai tây Lays tự nhiên',      15000,'8935049500138',N'/images/products/lays_classic.jpg',63,'g', N'Lát khoai tây vàng giòn tẩm muối tinh',1),
    (@ptSnack,  N'Đậu phộng Tân Tân vị nước cốt dừa', 18000,'8935049500145',N'/images/products/tan_tan_dua.jpg',100,'g', N'Hạt đậu phộng giòn bùi hương nước cốt dừa',1),
    (@ptHoa,    N'Nước rửa chén Sunlight chanh chai',  32000,'8935049500152',N'/images/products/sunlight_chanh.jpg',750,'ml',N'Tẩy sạch dầu mỡ từ tinh chất chanh tươi',1);
END
GO

-- Cấu hình sản phẩm thay thế (Coca ↔ Pepsi, TH ↔ Vinamilk, HảoHảo ↔ Omachi)
UPDATE p SET p.SubstituteProductID = p2.ProductID
FROM dbo.Products p, dbo.Products p2
WHERE p.Barcode='8935049500015' AND p2.Barcode='8935049500022' AND p.SubstituteProductID IS NULL;

UPDATE p SET p.SubstituteProductID = p2.ProductID
FROM dbo.Products p, dbo.Products p2
WHERE p.Barcode='8935049500022' AND p2.Barcode='8935049500015' AND p.SubstituteProductID IS NULL;

UPDATE p SET p.SubstituteProductID = p2.ProductID
FROM dbo.Products p, dbo.Products p2
WHERE p.Barcode='8935049500053' AND p2.Barcode='8935049500060' AND p.SubstituteProductID IS NULL;

UPDATE p SET p.SubstituteProductID = p2.ProductID
FROM dbo.Products p, dbo.Products p2
WHERE p.Barcode='8935049500084' AND p2.Barcode='8935049500091' AND p.SubstituteProductID IS NULL;
GO

-- ProductHealthTags (lookup by Barcode + TagName)
IF NOT EXISTS (SELECT 1 FROM dbo.ProductHealthTags)
INSERT INTO dbo.ProductHealthTags (ProductID, TagID)
SELECT p.ProductID, t.TagID FROM dbo.Products p, dbo.HealthTags t
WHERE (p.Barcode='8935049500039' AND t.TagName IN (N'Không đường', N'Ít béo'))
   OR (p.Barcode='8935049500060' AND t.TagName IN (N'Không đường', N'Ít béo', N'Organic Hữu cơ'))
   OR (p.Barcode='8935049500145' AND t.TagName=N'Dị ứng hạt lạc');
GO

-- 15. Slots (lookup ShelfLevelID via AisleCode + LevelNumber)
IF NOT EXISTS (SELECT 1 FROM dbo.Slots)
BEGIN
    -- Dãy B1 tầng 1,2,3: Nước ngọt
    INSERT INTO dbo.Slots (ShelfLevelID,SlotCode,ProductID,Quantity,LastScannedAt)
    SELECT sl.ShelfLevelID,'01',p.ProductID,15,GETUTCDATE()
    FROM dbo.ShelfLevels sl JOIN dbo.Aisles a ON sl.AisleID=a.AisleID
    JOIN dbo.Products p ON p.Barcode='8935049500015'
    WHERE a.AisleCode='B1' AND sl.LevelNumber=1;
    
    INSERT INTO dbo.Slots (ShelfLevelID,SlotCode,ProductID,Quantity,LastScannedAt)
    SELECT sl.ShelfLevelID,'02',p.ProductID,8,GETUTCDATE()
    FROM dbo.ShelfLevels sl JOIN dbo.Aisles a ON sl.AisleID=a.AisleID
    JOIN dbo.Products p ON p.Barcode='8935049500022'
    WHERE a.AisleCode='B1' AND sl.LevelNumber=1;
    
    INSERT INTO dbo.Slots (ShelfLevelID,SlotCode,ProductID,Quantity,LastScannedAt)
    SELECT sl.ShelfLevelID,'01',p.ProductID,22,GETUTCDATE()
    FROM dbo.ShelfLevels sl JOIN dbo.Aisles a ON sl.AisleID=a.AisleID
    JOIN dbo.Products p ON p.Barcode='8935049500039'
    WHERE a.AisleCode='B1' AND sl.LevelNumber=2;
    
    INSERT INTO dbo.Slots (ShelfLevelID,SlotCode,ProductID,Quantity,LastScannedAt)
    SELECT sl.ShelfLevelID,'01',p.ProductID,14,GETUTCDATE()
    FROM dbo.ShelfLevels sl JOIN dbo.Aisles a ON sl.AisleID=a.AisleID
    JOIN dbo.Products p ON p.Barcode='8935049500046'
    WHERE a.AisleCode='B1' AND sl.LevelNumber=3;

    -- Dãy B2: Sữa
    INSERT INTO dbo.Slots (ShelfLevelID,SlotCode,ProductID,Quantity,LastScannedAt)
    SELECT sl.ShelfLevelID,'01',p.ProductID,10,GETUTCDATE()
    FROM dbo.ShelfLevels sl JOIN dbo.Aisles a ON sl.AisleID=a.AisleID
    JOIN dbo.Products p ON p.Barcode='8935049500053'
    WHERE a.AisleCode='B2' AND sl.LevelNumber=1;
    
    INSERT INTO dbo.Slots (ShelfLevelID,SlotCode,ProductID,Quantity,LastScannedAt)
    SELECT sl.ShelfLevelID,'01',p.ProductID,2,GETUTCDATE()  -- Sắp hết hàng!
    FROM dbo.ShelfLevels sl JOIN dbo.Aisles a ON sl.AisleID=a.AisleID
    JOIN dbo.Products p ON p.Barcode='8935049500060'
    WHERE a.AisleCode='B2' AND sl.LevelNumber=2;
    
    INSERT INTO dbo.Slots (ShelfLevelID,SlotCode,ProductID,Quantity,LastScannedAt)
    SELECT sl.ShelfLevelID,'01',p.ProductID,30,GETUTCDATE()
    FROM dbo.ShelfLevels sl JOIN dbo.Aisles a ON sl.AisleID=a.AisleID
    JOIN dbo.Products p ON p.Barcode='8935049500077'
    WHERE a.AisleCode='B2' AND sl.LevelNumber=3;

    -- Dãy E1: Mì khô
    INSERT INTO dbo.Slots (ShelfLevelID,SlotCode,ProductID,Quantity,LastScannedAt)
    SELECT sl.ShelfLevelID,'01',p.ProductID,45,GETUTCDATE()
    FROM dbo.ShelfLevels sl JOIN dbo.Aisles a ON sl.AisleID=a.AisleID
    JOIN dbo.Products p ON p.Barcode='8935049500084'
    WHERE a.AisleCode='E1' AND sl.LevelNumber=1;
    
    INSERT INTO dbo.Slots (ShelfLevelID,SlotCode,ProductID,Quantity,LastScannedAt)
    SELECT sl.ShelfLevelID,'02',p.ProductID,20,GETUTCDATE()
    FROM dbo.ShelfLevels sl JOIN dbo.Aisles a ON sl.AisleID=a.AisleID
    JOIN dbo.Products p ON p.Barcode='8935049500091'
    WHERE a.AisleCode='E1' AND sl.LevelNumber=1;
    
    INSERT INTO dbo.Slots (ShelfLevelID,SlotCode,ProductID,Quantity,LastScannedAt)
    SELECT sl.ShelfLevelID,'01',p.ProductID,5,GETUTCDATE()
    FROM dbo.ShelfLevels sl JOIN dbo.Aisles a ON sl.AisleID=a.AisleID
    JOIN dbo.Products p ON p.Barcode='8935049500107'
    WHERE a.AisleCode='E1' AND sl.LevelNumber=2;

    -- Dãy D1: Bánh kẹo
    INSERT INTO dbo.Slots (ShelfLevelID,SlotCode,ProductID,Quantity,LastScannedAt)
    SELECT sl.ShelfLevelID,'01',p.ProductID,16,GETUTCDATE()
    FROM dbo.ShelfLevels sl JOIN dbo.Aisles a ON sl.AisleID=a.AisleID
    JOIN dbo.Products p ON p.Barcode='8935049500114'
    WHERE a.AisleCode='D1' AND sl.LevelNumber=1;
    
    INSERT INTO dbo.Slots (ShelfLevelID,SlotCode,ProductID,Quantity,LastScannedAt)
    SELECT sl.ShelfLevelID,'01',p.ProductID,12,GETUTCDATE()
    FROM dbo.ShelfLevels sl JOIN dbo.Aisles a ON sl.AisleID=a.AisleID
    JOIN dbo.Products p ON p.Barcode='8935049500121'
    WHERE a.AisleCode='D1' AND sl.LevelNumber=2;
    
    INSERT INTO dbo.Slots (ShelfLevelID,SlotCode,ProductID,Quantity,LastScannedAt)
    SELECT sl.ShelfLevelID,'01',p.ProductID,0,GETUTCDATE()  -- Hết hàng!
    FROM dbo.ShelfLevels sl JOIN dbo.Aisles a ON sl.AisleID=a.AisleID
    JOIN dbo.Products p ON p.Barcode='8935049500138'
    WHERE a.AisleCode='D1' AND sl.LevelNumber=3;

    -- Dãy D2: Đậu phộng
    INSERT INTO dbo.Slots (ShelfLevelID,SlotCode,ProductID,Quantity,LastScannedAt)
    SELECT sl.ShelfLevelID,'01',p.ProductID,25,GETUTCDATE()
    FROM dbo.ShelfLevels sl JOIN dbo.Aisles a ON sl.AisleID=a.AisleID
    JOIN dbo.Products p ON p.Barcode='8935049500145'
    WHERE a.AisleCode='D2' AND sl.LevelNumber=1;

    -- Dãy C1: Hóa phẩm
    INSERT INTO dbo.Slots (ShelfLevelID,SlotCode,ProductID,Quantity,LastScannedAt)
    SELECT sl.ShelfLevelID,'01',p.ProductID,19,GETUTCDATE()
    FROM dbo.ShelfLevels sl JOIN dbo.Aisles a ON sl.AisleID=a.AisleID
    JOIN dbo.Products p ON p.Barcode='8935049500152'
    WHERE a.AisleCode='C1' AND sl.LevelNumber=1;
END
GO

-- ExpiryDate + Supplier
UPDATE s SET s.ExpiryDate=CAST(DATEADD(day,30, GETUTCDATE()) AS DATE), s.Supplier=N'Vinamilk'
FROM dbo.Slots s JOIN dbo.Products p ON s.ProductID=p.ProductID
WHERE p.Barcode IN ('8935049500053','8935049500060','8935049500077');

UPDATE s SET s.ExpiryDate=CAST(DATEADD(day,180,GETUTCDATE()) AS DATE), s.Supplier=N'Acecook Việt Nam'
FROM dbo.Slots s JOIN dbo.Products p ON s.ProductID=p.ProductID
WHERE p.Barcode IN ('8935049500084','8935049500091');

UPDATE s SET s.ExpiryDate=CAST(DATEADD(day,365,GETUTCDATE()) AS DATE), s.Supplier=N'Tân Tân Food'
FROM dbo.Slots s JOIN dbo.Products p ON s.ProductID=p.ProductID
WHERE p.Barcode='8935049500145';
GO

-- 16. Robots & RobotZones
IF NOT EXISTS (SELECT 1 FROM dbo.Robots)
INSERT INTO dbo.Robots (RobotName,RobotCode,MacAddress,BatteryPct,Mode,IsOnline,LastSeenAt) VALUES
(N'SmartBot 4WD V1','ROBOT-01','30:AE:A4:07:0F:70',88,'idle',1,GETUTCDATE());
GO

IF NOT EXISTS (SELECT 1 FROM dbo.RobotZones)
INSERT INTO dbo.RobotZones (RobotID,ZoneID)
SELECT r.RobotID, z.ZoneID FROM dbo.Robots r, dbo.Zones z
WHERE r.RobotCode='ROBOT-01' AND z.ZoneCode IN ('B','D');
GO

-- 17. Maps & NavigationNodes & Edges
IF NOT EXISTS (SELECT 1 FROM dbo.Maps)
INSERT INTO dbo.Maps (FloorID,MapName,MapData,CreatedAt)
SELECT FloorID,N'Bản đồ Tầng 1 chính thức',N'{"grid_width":20,"grid_height":20,"obstacle_count":8}',GETUTCDATE()
FROM dbo.Floors WHERE FloorNumber=1;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.NavigationNodes)
BEGIN
    DECLARE @mapID INT;
    SELECT @mapID=MapID FROM dbo.Maps WHERE MapName LIKE N'Bản đồ%';
    DECLARE @aA1 INT,@aA2 INT,@aB1 INT,@aB2 INT,@aC1 INT;
    SELECT @aA1=AisleID FROM dbo.Aisles WHERE AisleCode='A1';
    SELECT @aA2=AisleID FROM dbo.Aisles WHERE AisleCode='A2';
    SELECT @aB1=AisleID FROM dbo.Aisles WHERE AisleCode='B1';
    SELECT @aB2=AisleID FROM dbo.Aisles WHERE AisleCode='B2';
    SELECT @aC1=AisleID FROM dbo.Aisles WHERE AisleCode='C1';
    INSERT INTO dbo.NavigationNodes (MapID,NodeName,XCoord,YCoord,NodeType,LinkedAisleID,IsBlocked) VALUES
    (@mapID,N'Cửa ra vào siêu thị (Entrance)', 0.0, 0.0,'entrance',NULL, 0),
    (@mapID,N'Trạm sạc tự động (Home Dock)',   1.0, 0.0,'station', NULL, 0),
    (@mapID,N'Ngã tư trung tâm Phân khu A',    3.0, 3.0,'intersection',NULL,0),
    (@mapID,N'Trước kệ trái cây dãy A1',       5.0, 3.0,'shelf_front',@aA1,0),
    (@mapID,N'Trước kệ rau củ dãy A2',         7.0, 3.0,'shelf_front',@aA2,0),
    (@mapID,N'Ngã tư lối đi dãy B1',           3.0, 7.0,'intersection',NULL,0),
    (@mapID,N'Trước kệ nước giải khát B1',     5.0, 7.0,'shelf_front',@aB1,0),
    (@mapID,N'Trước kệ sữa tươi dãy B2',       7.0, 7.0,'shelf_front',@aB2,0),
    (@mapID,N'Ngã ba hành lang hoá chất C1',   3.0,11.0,'intersection',NULL,0),
    (@mapID,N'Trước kệ rửa chén dãy C1',       5.0,11.0,'shelf_front',@aC1,0);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.NavigationEdges)
BEGIN
    DECLARE @n1 INT,@n2 INT,@n3 INT,@n4 INT,@n5 INT,@n6 INT,@n7 INT,@n8 INT,@n9 INT,@n10 INT;
    SELECT @n1=NodeID FROM dbo.NavigationNodes WHERE NodeName LIKE N'Cửa ra vào%';
    SELECT @n2=NodeID FROM dbo.NavigationNodes WHERE NodeName LIKE N'Trạm sạc%';
    SELECT @n3=NodeID FROM dbo.NavigationNodes WHERE NodeName LIKE N'Ngã tư trung%';
    SELECT @n4=NodeID FROM dbo.NavigationNodes WHERE NodeName LIKE N'Trước kệ trái%';
    SELECT @n5=NodeID FROM dbo.NavigationNodes WHERE NodeName LIKE N'Trước kệ rau%';
    SELECT @n6=NodeID FROM dbo.NavigationNodes WHERE NodeName LIKE N'Ngã tư lối%';
    SELECT @n7=NodeID FROM dbo.NavigationNodes WHERE NodeName LIKE N'Trước kệ nước%';
    SELECT @n8=NodeID FROM dbo.NavigationNodes WHERE NodeName LIKE N'Trước kệ sữa%';
    SELECT @n9=NodeID FROM dbo.NavigationNodes WHERE NodeName LIKE N'Ngã ba hành%';
    SELECT @n10=NodeID FROM dbo.NavigationNodes WHERE NodeName LIKE N'Trước kệ rửa%';
    INSERT INTO dbo.NavigationEdges (FromNodeID,ToNodeID,Distance,IsBidirectional) VALUES
    (@n1,@n2,1.0,1),(@n1,@n3,4.2,1),(@n3,@n4,2.0,1),(@n4,@n5,2.0,1),
    (@n3,@n6,4.0,1),(@n6,@n7,2.0,1),(@n7,@n8,2.0,1),
    (@n6,@n9,4.0,1),(@n9,@n10,2.0,1);
END
GO

-- 18. Workstations
IF NOT EXISTS (SELECT 1 FROM dbo.Workstations)
INSERT INTO dbo.Workstations (ZoneID,NodeID,StationName)
SELECT z.ZoneID, n.NodeID, N'Trạm sạc số 1 SmartMarket'
FROM dbo.Zones z, dbo.NavigationNodes n
WHERE z.ZoneCode='B' AND n.NodeName LIKE N'Trạm sạc%';
GO

-- 19. ShoppingHistories & HistoryItems
IF NOT EXISTS (SELECT 1 FROM dbo.ShoppingHistories)
BEGIN
    INSERT INTO dbo.ShoppingHistories (MemberID,ShoppingDate,TotalAmount,PaymentMethod)
    SELECT MemberID,DATEADD(day,-1,GETUTCDATE()),76000.00,'MOMO' FROM dbo.Members WHERE PhoneNumber='0782766322';

    INSERT INTO dbo.ShoppingHistories (MemberID,ShoppingDate,TotalAmount,PaymentMethod)
    SELECT MemberID,DATEADD(day,-5,GETUTCDATE()),90000.00,'VNPAY' FROM dbo.Members WHERE PhoneNumber='0868205403';

    DECLARE @sh1 INT, @sh2 INT;
    SELECT TOP 1 @sh1=ShoppingHistoryID FROM dbo.ShoppingHistories ORDER BY ShoppingHistoryID;
    SELECT TOP 1 @sh2=ShoppingHistoryID FROM dbo.ShoppingHistories ORDER BY ShoppingHistoryID DESC;

    INSERT INTO dbo.HistoryItems (ShoppingHistoryID,ProductID,Quantity,UnitPrice)
    SELECT @sh1,p.ProductID,2,10500.00 FROM dbo.Products p WHERE p.Barcode='8935049500039';
    INSERT INTO dbo.HistoryItems (ShoppingHistoryID,ProductID,Quantity,UnitPrice)
    SELECT @sh1,p.ProductID,1,55000.00 FROM dbo.Products p WHERE p.Barcode='8935049500114';
    INSERT INTO dbo.HistoryItems (ShoppingHistoryID,ProductID,Quantity,UnitPrice)
    SELECT @sh2,p.ProductID,20,4500.00 FROM dbo.Products p WHERE p.Barcode='8935049500084';
END
GO

-- 20. Recipes & RecipeItems
IF NOT EXISTS (SELECT 1 FROM dbo.Recipes)
BEGIN
    INSERT INTO dbo.Recipes (RecipeName,Description,YieldPortions,ImageUrl,Calories,HealthyScore,AlternativeSuggestion) VALUES
    (N'Món mì trộn chua cay đặc biệt',N'Công thức mì xốt trộn giòn bùi phối xúc xích rau xanh',2,N'/images/recipes/mi_tron.jpg',520,45,N'Thay mì tôm bằng mì khoai tây Omachi để giảm dầu mỡ');

    INSERT INTO dbo.RecipeItems (RecipeID,ProductID,QuantityRequired,UnitOfMeasure)
    SELECT r.RecipeID,p.ProductID,2.00,N'gói' FROM dbo.Recipes r, dbo.Products p
    WHERE r.RecipeName LIKE N'Món mì%' AND p.Barcode='8935049500084';

    INSERT INTO dbo.RecipeItems (RecipeID,ProductID,QuantityRequired,UnitOfMeasure)
    SELECT r.RecipeID,p.ProductID,0.50,N'gói' FROM dbo.Recipes r, dbo.Products p
    WHERE r.RecipeName LIKE N'Món mì%' AND p.Barcode='8935049500145';
END
GO

-- 21. Promotions
IF NOT EXISTS (SELECT 1 FROM dbo.Promotions)
BEGIN
    INSERT INTO dbo.Promotions (PromotionName,PromotionType,DiscountValue,StartDate,EndDate,IsActive) VALUES
    (N'Ưu đãi giải nhiệt mùa hè','discount',10.00,CAST(GETUTCDATE() AS DATE),CAST(DATEADD(month,2,GETUTCDATE()) AS DATE),1);

    INSERT INTO dbo.PromotionProducts (PromotionID,ProductID,Priority)
    SELECT pr.PromotionID,p.ProductID,1 FROM dbo.Promotions pr,dbo.Products p
    WHERE pr.PromotionName LIKE N'Ưu đãi%' AND p.Barcode='8935049500046';
    INSERT INTO dbo.PromotionProducts (PromotionID,ProductID,Priority)
    SELECT pr.PromotionID,p.ProductID,2 FROM dbo.Promotions pr,dbo.Products p
    WHERE pr.PromotionName LIKE N'Ưu đãi%' AND p.Barcode='8935049500015';
END
GO

-- 22. Brands & AdPackages
IF NOT EXISTS (SELECT 1 FROM dbo.Brands)
INSERT INTO dbo.Brands (BrandName,Description) VALUES
(N'Orion Vina',  N'Tập đoàn bánh kẹo Orion Vina Hàn Quốc'),
(N'TH True Milk',N'Công ty cổ phần thực phẩm sữa TH'),
(N'Vinamilk',    N'Công ty cổ phần sữa Việt Nam'),
(N'Acecook',     N'Công ty TNHH Acecook Việt Nam');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AdPackages)
INSERT INTO dbo.AdPackages (PackageName,Price,AdScore,TimeSlotStart,TimeSlotEnd,IsWeekendOnly) VALUES
(N'Gói Sáng Sớm (07:00-12:00)', 500000,85,'07:00:00','12:00:00',0),
(N'Gói Cả Ngày (All Day)',       800000,70,NULL,NULL,0),
(N'Gói Cuối Tuần',              1200000,100,NULL,NULL,1),
(N'Gói Giờ Vàng (17:00-21:00)', 700000,90,'17:00:00','21:00:00',0);
GO

-- 23. SponsoredProducts
IF NOT EXISTS (SELECT 1 FROM dbo.SponsoredProducts)
INSERT INTO dbo.SponsoredProducts (ProductID,BrandID,PackageID,StartDate,EndDate,Priority,IsActive)
SELECT p.ProductID,b.BrandID,ap.PackageID,CAST(GETUTCDATE() AS DATE),CAST(DATEADD(month,1,GETUTCDATE()) AS DATE),5,1
FROM dbo.Products p, dbo.Brands b, dbo.AdPackages ap
WHERE p.Barcode='8935049500121' AND b.BrandName=N'Orion Vina' AND ap.PackageName LIKE N'Gói Sáng%';
GO

-- 24. ForbiddenZones
IF NOT EXISTS (SELECT 1 FROM dbo.ForbiddenZones)
INSERT INTO dbo.ForbiddenZones (MapID,ZoneName,XMin,YMin,XMax,YMax,IsActive,Reason)
SELECT m.MapID,N'Khu vực quầy thu ngân',8.0,0.0,12.0,3.0,1,N'Robot không được đi gần quầy thu ngân khi có khách'
FROM dbo.Maps m WHERE m.MapName LIKE N'Bản đồ%'
UNION ALL
SELECT m.MapID,N'Hành lang thoát hiểm',0.0,8.0,2.0,20.0,1,N'Lối thoát hiểm bắt buộc luôn thông thoáng'
FROM dbo.Maps m WHERE m.MapName LIKE N'Bản đồ%';
GO

-- 25. MemberAlerts
IF NOT EXISTS (SELECT 1 FROM dbo.MemberAlerts)
INSERT INTO dbo.MemberAlerts (MemberID,AlertType,AlertMessage,IsRead)
SELECT m.MemberID,'Allergy',N'⚠️ CẢNH BÁO DỊ ỨNG: Đậu phộng Tân Tân chứa hạt lạc — Hùng đã được ghi nhận dị ứng hạt lạc!',0
FROM dbo.Members m WHERE m.PhoneNumber='0868205403'
UNION ALL
SELECT m.MemberID,'BudgetExceeded',N'💰 Tổng giỏ hàng 210.000đ đã vượt ngân sách 200.000đ đã cài đặt.',0
FROM dbo.Members m WHERE m.PhoneNumber='0782766322';
GO

-- 26. MemberEvents
IF NOT EXISTS (SELECT 1 FROM dbo.MemberEvents)
INSERT INTO dbo.MemberEvents (MemberID,EventName,EventDate,DiscountPct,IsProcessed)
SELECT m.MemberID,'Birthday',CAST(DATEADD(day,3,GETUTCDATE()) AS DATE),15.00,0
FROM dbo.Members m WHERE m.PhoneNumber='0782766322'
UNION ALL
SELECT m.MemberID,'Anniversary',CAST(DATEADD(day,7,GETUTCDATE()) AS DATE),10.00,0
FROM dbo.Members m WHERE m.PhoneNumber='0868205403';
GO

-- ============================================================================
PRINT '====================================================================';
PRINT '  SUCCESS: SEED DATA applied to SmartMarketBot (Azure SQL)!';
PRINT '  4 SQL Views | 4 Accounts | 15 Products | 10 Nav Nodes | All done!';
PRINT '====================================================================';
GO
