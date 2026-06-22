-- ============================================================
-- Fix script 2: Insert INVOICE_HISTORY/ITEM, MEAL_SUGGESTION/ITEM, AISLE_SCAN
-- ============================================================

USE SuperMarketBot;
GO

-- ══════════════════════════════════════════════════════════════
-- 25. INVOICE_HISTORY (25 record: ID 1-25)
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.INVOICE_HISTORY ON;
DECLARE @m INT = 1;
DECLARE @total DECIMAL(18,2);
DECLARE @invoiceId INT = 1;
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
-- 26. INVOICE_HISTORY_ITEM (70 record)
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.INVOICE_HISTORY_ITEM ON;
DECLARE @inv INT = 1;
DECLARE @items INT;
DECLARE @itemId INT = 1;
DECLARE @productId INT;
DECLARE @price DECIMAL(18,2);
WHILE @inv <= 25
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
-- 27. MEAL_SUGGESTION (9 record: ID 1-9)
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.MEAL_SUGGESTION ON;
INSERT INTO dbo.MEAL_SUGGESTION (MealSuggestionID, MealName, Description, YieldPortions, ImageUrl, Calories, healthy_score, alternative_suggestion) VALUES
(1, N'Cơm chiên trứng', N'Cơm chiên với trứng và hành lá', 2, NULL, 480, 65, N'Có thể thêm rau xanh'),
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
-- 28. MEAL_ITEM (27 record: 3 sản phẩm mỗi meal)
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.MEAL_ITEM ON;
DECLARE @mid INT = 1;
DECLARE @miId INT = 1;
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
-- 29. AISLE_SCAN (7 record: ID 1-7)
-- ══════════════════════════════════════════════════════════════
SET IDENTITY_INSERT dbo.AISLE_SCAN ON;
INSERT INTO dbo.AISLE_SCAN (ScanID, AisleID, RobotID, ScannedAt, EmptyPercentage, NeedsRestock, ImageUrl) VALUES
(1, 1, 1, DATEADD(hour, -1, DATEADD(hour, 7, GETUTCDATE())), 12.50, 0, NULL),
(2, 3, 2, DATEADD(hour, -2, DATEADD(hour, 7, GETUTCDATE())), 15.50, 0, NULL),
(3, 4, 3, DATEADD(hour, -3, DATEADD(hour, 7, GETUTCDATE())), 45.00, 1, NULL),
(4, 5, 2, DATEADD(hour, -4, DATEADD(hour, 7, GETUTCDATE())), 8.20, 0, NULL),
(5, 6, 5, DATEADD(hour, -5, DATEADD(hour, 7, GETUTCDATE())), 22.30, 0, NULL),
(6, 7, 3, DATEADD(hour, -6, DATEADD(hour, 7, GETUTCDATE())), 67.00, 1, NULL),
(7, 8, 2, DATEADD(hour, -7, DATEADD(hour, 7, GETUTCDATE())), 5.00, 0, NULL);
SET IDENTITY_INSERT dbo.AISLE_SCAN OFF;
GO

PRINT '✅ Inserted INVOICE_HISTORY (25), INVOICE_HISTORY_ITEM (70), MEAL_SUGGESTION (9), MEAL_ITEM (27), AISLE_SCAN (7).';
GO
