-- ============================================================
-- Migration: Add MEMBER_NOTIFICATION table
-- Date: 2026-06-25
-- Desc: Lưu lịch sử cảnh báo scan item và thông báo realtime
--       để member xem lại và đánh dấu đã đọc
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_NAME = 'MEMBER_NOTIFICATION'
)
BEGIN
    CREATE TABLE MEMBER_NOTIFICATION (
        NotificationID  INT IDENTITY(1,1) PRIMARY KEY,
        MemberID        INT            NOT NULL,
        NotifType       NVARCHAR(50)   NOT NULL,   -- Allergy | BudgetExceeded | DuplicatePurchase | CartUpdate | PointsEarned
        Title           NVARCHAR(200)  NOT NULL,
        Message         NVARCHAR(MAX)  NOT NULL,
        PayloadJson     NVARCHAR(MAX)  NULL,        -- JSON tùy loại
        IsRead          BIT            NOT NULL DEFAULT 0,
        CreatedAt       DATETIME2      NOT NULL DEFAULT DATEADD(hour, 7, GETUTCDATE()),
        CONSTRAINT FK_MN_MEMBER FOREIGN KEY (MemberID)
            REFERENCES MEMBER(MemberID) ON DELETE CASCADE
    );

    CREATE INDEX IX_MN_MemberID_IsRead  ON MEMBER_NOTIFICATION(MemberID, IsRead);
    CREATE INDEX IX_MN_CreatedAt        ON MEMBER_NOTIFICATION(CreatedAt DESC);

    PRINT 'Table MEMBER_NOTIFICATION created successfully.';
END
ELSE
BEGIN
    PRINT 'Table MEMBER_NOTIFICATION already exists. Skipped.';
END
