-- ============================================================
-- Migration: Add AvatarUrl to ACCOUNT table
-- Date: 2026-06-25
-- Desc: Thêm cột AvatarUrl để lưu URL ảnh đại diện hiển thị UI
--       (tách biệt với MEMBER.FacePath dùng cho AI face recognition)
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'ACCOUNT' AND COLUMN_NAME = 'AvatarUrl'
)
BEGIN
    ALTER TABLE ACCOUNT
    ADD AvatarUrl NVARCHAR(500) NULL;

    PRINT 'Column AvatarUrl added to ACCOUNT table.';
END
ELSE
BEGIN
    PRINT 'Column AvatarUrl already exists in ACCOUNT table. Skipped.';
END
