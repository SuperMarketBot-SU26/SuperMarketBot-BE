-- ============================================================
-- SmartMarketBot — Phase B Line-Nav Refactor (Step 1/4)
-- Thêm cột NodeCode vào NAVIGATION_NODE cho line-scanning nav.
-- Run SAU khi DB đã có schema V4.1 (erd_database.sql đã chạy).
-- Idempotent — chạy lại nhiều lần không lỗi.
-- ============================================================

USE SuperMarketBot;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 1. Thêm cột nếu chưa có (idempotent).
IF COL_LENGTH('dbo.NAVIGATION_NODE', 'NodeCode') IS NULL
BEGIN
    ALTER TABLE dbo.NAVIGATION_NODE
        ADD NodeCode NVARCHAR(50) NOT NULL
            CONSTRAINT DF_NAVIGATION_NODE_NodeCode DEFAULT '';
    PRINT '✅ Added NodeCode column to NAVIGATION_NODE';
END
ELSE
BEGIN
    PRINT '⚠️  NodeCode column already exists — skipping ADD.';
END
GO

-- 2. Drop default constraint tạm (nếu có) để backfill an toàn.
DECLARE @constraintName NVARCHAR(200);
SELECT @constraintName = name
FROM sys.default_constraints
WHERE parent_object_id = OBJECT_ID('dbo.NAVIGATION_NODE')
  AND parent_column_id = COLUMNPROPERTY(OBJECT_ID('dbo.NAVIGATION_NODE'), 'NodeCode', 'ColumnId');

IF @constraintName IS NOT NULL
BEGIN
    EXEC('ALTER TABLE dbo.NAVIGATION_NODE DROP CONSTRAINT ' + @constraintName);
    PRINT '🗑  Dropped default constraint: ' + @constraintName;
END
GO

-- 3. Backfill: mỗi row cũ nhận NodeCode = 'NODE_{NodeId}'.
--    Đảm bảo uniqueness vì NodeId là PK.
UPDATE dbo.NAVIGATION_NODE
SET NodeCode = 'NODE_' + CAST(NodeId AS NVARCHAR(10))
WHERE NodeCode IS NULL OR NodeCode = '';
PRINT '✅ Backfilled NodeCode for existing navigation nodes.';
GO

-- 4. Unique index per map (Phase B: cho phép cùng code ở map khác).
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_NAVIGATION_NODE_MapID_NodeCode'
      AND object_id = OBJECT_ID('dbo.NAVIGATION_NODE')
)
BEGIN
    CREATE UNIQUE INDEX IX_NAVIGATION_NODE_MapID_NodeCode
        ON dbo.NAVIGATION_NODE (MapID, NodeCode);
    PRINT '✅ Created unique index IX_NAVIGATION_NODE_MapID_NodeCode';
END
ELSE
BEGIN
    PRINT '⚠️  Index already exists — skipping CREATE.';
END
GO

PRINT '🎉 Phase B Step 1 complete: NodeCode column ready for line-scanning nav.';
GO
