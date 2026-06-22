-- ============================================================
-- Fix script 3: Insert MEAL_ITEM (các bảng còn lại đã có data)
-- ============================================================
USE SuperMarketBot;
GO

-- MEAL_ITEM (27 record)
SET IDENTITY_INSERT dbo.MEAL_ITEM ON;
DECLARE @mid INT = 1;
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
        SET @c = @c + 1;
    END
    SET @mid = @mid + 1;
END
SET IDENTITY_INSERT dbo.MEAL_ITEM OFF;
GO

PRINT '✅ Inserted MEAL_ITEM (27 records).';
GO
