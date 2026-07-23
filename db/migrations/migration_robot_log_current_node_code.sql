-- Migration: Add CurrentNodeCode to ROBOT_LOG
-- Phase B Step 2 — line-scan navigation: firmware chỉ gửi NodeCode (RFID/QR/tape-line)
-- về BE. BE lưu raw vào log để debug trajectory; dùng cho Phase 4 (ad engine).
-- Idempotent: an toàn chạy lại nhiều lần.

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'ROBOT_LOG' AND COLUMN_NAME = 'CurrentNodeCode'
)
BEGIN
    ALTER TABLE ROBOT_LOG ADD CurrentNodeCode NVARCHAR(50) NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ROBOT_LOG_CurrentNodeCode' AND object_id = OBJECT_ID('ROBOT_LOG')
)
BEGIN
    CREATE INDEX IX_ROBOT_LOG_CurrentNodeCode ON ROBOT_LOG(CurrentNodeCode);
END
GO
