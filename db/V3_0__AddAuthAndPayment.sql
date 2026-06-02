/*
  V3.0 — Auth & Payment (idempotent)
  Chạy trên DB SuperMarketBot đã có sẵn (database.sql).
  Thêm: EmailOtps, UserTokens, Payments + cột mới trên Users.
*/
USE SuperMarketBot;
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ── Users: cột mới ──────────────────────────────────────────────
IF COL_LENGTH('dbo.Users', 'EmailConfirmed') IS NULL
    ALTER TABLE dbo.Users ADD EmailConfirmed BIT NOT NULL
        CONSTRAINT DF_Users_EmailConfirmed DEFAULT 0;
GO

IF COL_LENGTH('dbo.Users', 'FullName') IS NULL
    ALTER TABLE dbo.Users ADD FullName NVARCHAR(100) NULL;
GO

IF COL_LENGTH('dbo.Users', 'AvatarUrl') IS NULL
    ALTER TABLE dbo.Users ADD AvatarUrl NVARCHAR(500) NULL;
GO

IF COL_LENGTH('dbo.Users', 'UpdatedAt') IS NULL
    ALTER TABLE dbo.Users ADD UpdatedAt DATETIME2 NULL;
GO

-- Unique Email (chỉ khi có giá trị, cho phép nhiều NULL)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_Email' AND object_id = OBJECT_ID('dbo.Users'))
    CREATE UNIQUE NONCLUSTERED INDEX IX_Users_Email ON dbo.Users(Email)
        WHERE Email IS NOT NULL;
GO

-- ── EmailOtps ───────────────────────────────────────────────────
IF OBJECT_ID('dbo.EmailOtps', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmailOtps (
        OtpId                 UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_EmailOtps_OtpId DEFAULT NEWID(),
        Email                 NVARCHAR(256)    NOT NULL,
        OtpCode               NVARCHAR(6)      NOT NULL,
        OtpType               NVARCHAR(50)     NOT NULL CONSTRAINT DF_EmailOtps_OtpType DEFAULT 'Registration',
        ExpiredAt             DATETIME2        NOT NULL,
        IsUsed                BIT              NOT NULL CONSTRAINT DF_EmailOtps_IsUsed DEFAULT 0,
        CreatedAt             DATETIME2        NOT NULL CONSTRAINT DF_EmailOtps_CreatedAt DEFAULT GETUTCDATE(),
        TemporaryPasswordHash NVARCHAR(MAX)    NULL,
        TemporaryFullName     NVARCHAR(100)    NULL,
        TemporaryPhone        NVARCHAR(20)     NULL,
        CONSTRAINT PK_EmailOtps PRIMARY KEY (OtpId)
    );

    EXEC('CREATE NONCLUSTERED INDEX IX_EmailOtps_Email_OtpType_IsUsed ON dbo.EmailOtps(Email, OtpType, IsUsed) WHERE IsUsed = 0');
    EXEC('CREATE NONCLUSTERED INDEX IX_EmailOtps_ExpiredAt ON dbo.EmailOtps(ExpiredAt) WHERE IsUsed = 0');
END
GO

-- ── UserTokens (refresh token) ──────────────────────────────────
IF OBJECT_ID('dbo.UserTokens', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserTokens (
        TokenId       UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_UserTokens_TokenId DEFAULT NEWID(),
        UserId        INT              NOT NULL,
        RefreshToken  NVARCHAR(512)    NOT NULL,
        ExpiryDate    DATETIME2        NOT NULL,
        IsRevoked     BIT              NOT NULL CONSTRAINT DF_UserTokens_IsRevoked DEFAULT 0,
        CreatedAt     DATETIME2        NOT NULL CONSTRAINT DF_UserTokens_CreatedAt DEFAULT GETUTCDATE(),
        DeviceInfo    NVARCHAR(256)    NULL,
        IpAddress     NVARCHAR(64)     NULL,
        CONSTRAINT PK_UserTokens PRIMARY KEY (TokenId),
        CONSTRAINT FK_UserTokens_Users_UserId FOREIGN KEY (UserId)
            REFERENCES dbo.Users(UserID) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_UserTokens_RefreshToken ON dbo.UserTokens(RefreshToken);
    CREATE NONCLUSTERED INDEX IX_UserTokens_UserId_IsRevoked ON dbo.UserTokens(UserId, IsRevoked);
END
GO

-- ── Payments (SePay) ────────────────────────────────────────────
IF OBJECT_ID('dbo.Payments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Payments (
        PaymentId          UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Payments_PaymentId DEFAULT NEWID(),
        UserId             INT              NOT NULL,
        OrderCode          NVARCHAR(100)    NOT NULL,
        Amount             DECIMAL(18,2)    NOT NULL,
        Currency           NVARCHAR(10)     NOT NULL CONSTRAINT DF_Payments_Currency DEFAULT 'VND',
        Status             NVARCHAR(20)     NOT NULL CONSTRAINT DF_Payments_Status DEFAULT 'Pending',
        Description        NVARCHAR(500)    NULL,
        QrCodeUrl          NVARCHAR(1000)   NULL,
        SepayTransactionId NVARCHAR(100)    NULL,
        WebhookPayload     NVARCHAR(MAX)    NULL,
        CreatedAt          DATETIME2        NOT NULL CONSTRAINT DF_Payments_CreatedAt DEFAULT GETUTCDATE(),
        PaidAt             DATETIME2        NULL,
        CONSTRAINT PK_Payments PRIMARY KEY (PaymentId),
        CONSTRAINT FK_Payments_Users_UserId FOREIGN KEY (UserId)
            REFERENCES dbo.Users(UserID) ON DELETE CASCADE
    );

    CREATE UNIQUE NONCLUSTERED INDEX IX_Payments_OrderCode ON dbo.Payments(OrderCode);
    CREATE NONCLUSTERED INDEX IX_Payments_UserId_Status ON dbo.Payments(UserId, Status);
END
GO

-- ── EF migrations history (baseline — tránh ef update tạo lại toàn bộ) ──
IF OBJECT_ID('dbo.__EFMigrationsHistory', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.[__EFMigrationsHistory] (
        MigrationId    NVARCHAR(150) NOT NULL,
        ProductVersion NVARCHAR(32)  NOT NULL,
        CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY (MigrationId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.[__EFMigrationsHistory] WHERE MigrationId = N'20260602131847_AddAuthAndPayment')
    INSERT INTO dbo.[__EFMigrationsHistory] (MigrationId, ProductVersion)
    VALUES (N'20260602131847_AddAuthAndPayment', N'10.0.0');
GO

PRINT 'V3_0__AddAuthAndPayment applied successfully.';
GO
