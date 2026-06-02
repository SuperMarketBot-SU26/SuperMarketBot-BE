# SmartMarketBot-BE — Azure Deployment (A–Z)

## 1. Chuẩn bị tài khoản Azure

1. Đăng nhập [portal.azure.com](https://portal.azure.com)
2. Tạo **Resource Group**: `rg-smartmarketbot` (region: Southeast Asia)

---

## 2. Azure SQL Database

### 2a. Tạo SQL Server
```
Resource: SQL Server
Name: sql-smartmarketbot
Admin login: sqladmin
Password: <mật khẩu mạnh — lưu vào Key Vault>
Region: Southeast Asia
```

### 2b. Tạo SQL Database
```
Name: SmartMarketBot
Pricing tier: Basic (5 DTU) hoặc General Purpose Serverless (dev)
Max size: 2 GB
```

### 2c. Cấu hình Firewall
- Thêm IP máy local để chạy migration
- Allow Azure services: **Yes**

### 2d. Chạy EF Core Migration
```powershell
# Trong thư mục SuperMarketBot-BE
$env:ConnectionStrings__DefaultConnection = "Server=sql-smartmarketbot.database.windows.net;Database=SmartMarketBot;User Id=sqladmin;Password=<password>;TrustServerCertificate=False;Encrypt=True;"

dotnet ef database update --project src/SmartMarketBot.Infrastructure --startup-project src/SmartMarketBot.API
```

---

## 3. Azure App Service (API)

### 3a. Tạo App Service Plan
```
Name: asp-smartmarketbot
OS: Linux
Pricing: B1 (Basic, ~$13/tháng) hoặc F1 (Free — giới hạn)
```

### 3b. Tạo Web App
```
Name: smartmarketbot-api        ← phải khớp AZURE_WEBAPP_NAME trong workflow
Runtime: .NET 10
OS: Linux
Region: Southeast Asia
```

### 3c. Application Settings (quan trọng — thay vì lưu secrets trong code)

Vào **Configuration → Application Settings**, thêm:

| Name | Value |
|------|-------|
| `ConnectionStrings__DefaultConnection` | `Server=sql-smartmarketbot.database.windows.net;Database=SmartMarketBot;User Id=sqladmin;Password=xxx;TrustServerCertificate=False;Encrypt=True;` |
| `Jwt__SecretKey` | `<random 64 ký tự>` |
| `Jwt__Issuer` | `SmartMarketBot` |
| `Jwt__Audience` | `SmartMarketBot.Client` |
| `Jwt__AccessTokenExpiryMinutes` | `15` |
| `Jwt__RefreshTokenExpiryDays` | `7` |
| `Email__SmtpHost` | `smtp.gmail.com` |
| `Email__SmtpPort` | `587` |
| `Email__SmtpUser` | `your@gmail.com` |
| `Email__SmtpPassword` | `<Gmail App Password>` |
| `Email__FromEmail` | `your@gmail.com` |
| `Email__FromName` | `SmartMarketBot` |
| `SePay__ApiKey` | `<SePay API Key>` |
| `SePay__WebhookSecret` | `<SePay Webhook Secret>` |
| `SePay__Bank__Code` | `MB` |
| `SePay__Bank__AccountNumber` | `<số tài khoản>` |
| `SePay__Bank__AccountName` | `<tên chủ tài khoản>` |
| `SePay__Enabled` | `true` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

> **Lưu ý:** Azure dùng `__` (double underscore) thay vì `:` để phân cấp config.

---

## 4. GitHub Actions (CI/CD)

### 4a. Lấy Publish Profile
- Azure Portal → App Service → **Get publish profile** → download file XML

### 4b. Thêm Secret vào GitHub
- Repo → Settings → Secrets → Actions → New secret
- Name: `AZURE_WEBAPP_PUBLISH_PROFILE`
- Value: nội dung file XML vừa download

### 4c. Push lên main
```bash
git push origin main
```
→ GitHub Actions sẽ tự động build & deploy.

---

## 5. EF Core Migration (lần đầu / mỗi khi thêm entity)

```powershell
# Tạo migration mới
dotnet ef migrations add AddAuthAndPayment `
  --project src/SmartMarketBot.Infrastructure `
  --startup-project src/SmartMarketBot.API

# Áp dụng lên Azure SQL
dotnet ef database update `
  --project src/SmartMarketBot.Infrastructure `
  --startup-project src/SmartMarketBot.API `
  --connection "Server=sql-smartmarketbot.database.windows.net;..."
```

---

## 6. Seed Data cần thiết

Sau khi migration, insert Role mặc định:
```sql
INSERT INTO Roles (RoleName) VALUES ('Member'), ('Staff'), ('Admin');
```

---

## 7. Cấu hình Gmail App Password (SMTP)

1. Bật 2FA cho Gmail
2. Google Account → Security → App Passwords
3. Tạo password cho app "Mail" → copy 16 ký tự
4. Paste vào `Email__SmtpPassword` trong Azure App Settings

---

## 8. Cấu hình SePay

1. Đăng ký tại [sepay.vn](https://sepay.vn)
2. Thêm tài khoản ngân hàng nhận tiền
3. Lấy **API Key** và **Webhook Secret** từ dashboard
4. Cấu hình Webhook URL: `https://smartmarketbot-api.azurewebsites.net/api/payments/sepay/webhook`
5. SePay yêu cầu HTTPS — Azure App Service đã có sẵn SSL

---

## 9. Kiểm tra sau deploy

```bash
# Health check
curl https://smartmarketbot-api.azurewebsites.net/health

# Scalar API docs (dev only)
# https://smartmarketbot-api.azurewebsites.net/scalar/v1
```

---

## 10. Sơ đồ API Auth

```
POST /api/auth/register          → Gửi OTP email
POST /api/auth/verify-otp        → Xác thực OTP → JWT + RefreshToken
POST /api/auth/resend-otp        → Gửi lại OTP
POST /api/auth/login             → Đăng nhập → JWT + RefreshToken
POST /api/auth/refresh           → Làm mới Access Token
POST /api/auth/logout            → Revoke Refresh Token [Authorize]
POST /api/auth/forgot-password   → Gửi OTP reset password
POST /api/auth/reset-password    → Đặt mật khẩu mới qua OTP

POST /api/payments/create                 → Tạo đơn SePay [Authorize]
GET  /api/payments/status/{orderCode}     → Kiểm tra trạng thái [Authorize]
POST /api/payments/sepay/webhook          → IPN từ SePay [public + Apikey auth]
```

---

## 11. Database Schema — Auth & Payment

```sql
-- Thêm vào bảng Users (đã có)
ALTER TABLE Users ADD
  FullName      NVARCHAR(100) NULL,
  EmailConfirmed BIT NOT NULL DEFAULT 0,
  AvatarUrl     NVARCHAR(500) NULL,
  UpdatedAt     DATETIME2 NULL;
CREATE UNIQUE INDEX IX_Users_Email ON Users(Email) WHERE Email IS NOT NULL;

-- Bảng mới: UserTokens (refresh token)
CREATE TABLE UserTokens (
  TokenId       UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
  UserId        INT NOT NULL REFERENCES Users(UserID) ON DELETE CASCADE,
  RefreshToken  NVARCHAR(512) NOT NULL,
  ExpiryDate    DATETIME2 NOT NULL,
  IsRevoked     BIT NOT NULL DEFAULT 0,
  CreatedAt     DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
  DeviceInfo    NVARCHAR(256) NULL,
  IpAddress     NVARCHAR(64) NULL
);
CREATE INDEX IX_UserTokens_RefreshToken ON UserTokens(RefreshToken);
CREATE INDEX IX_UserTokens_UserId_IsRevoked ON UserTokens(UserId, IsRevoked);

-- Bảng mới: EmailOtps
CREATE TABLE EmailOtps (
  OtpId                 UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
  Email                 NVARCHAR(256) NOT NULL,
  OtpCode               NVARCHAR(6) NOT NULL,
  OtpType               NVARCHAR(50) NOT NULL DEFAULT 'Registration',
  ExpiredAt             DATETIME2 NOT NULL,
  IsUsed                BIT NOT NULL DEFAULT 0,
  CreatedAt             DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
  TemporaryPasswordHash NVARCHAR(MAX) NULL,
  TemporaryFullName     NVARCHAR(100) NULL,
  TemporaryPhone        NVARCHAR(20) NULL
);
CREATE INDEX IX_EmailOtps_Email_Type_IsUsed ON EmailOtps(Email, OtpType, IsUsed) WHERE IsUsed = 0;
CREATE INDEX IX_EmailOtps_ExpiredAt ON EmailOtps(ExpiredAt) WHERE IsUsed = 0;

-- Bảng mới: Payments (SePay)
CREATE TABLE Payments (
  PaymentId          UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
  UserId             INT NOT NULL REFERENCES Users(UserID) ON DELETE CASCADE,
  OrderCode          NVARCHAR(100) NOT NULL UNIQUE,
  Amount             DECIMAL(18,2) NOT NULL,
  Currency           NVARCHAR(10) NOT NULL DEFAULT 'VND',
  Status             NVARCHAR(20) NOT NULL DEFAULT 'Pending',
  Description        NVARCHAR(500) NULL,
  QrCodeUrl          NVARCHAR(1000) NULL,
  SepayTransactionId NVARCHAR(100) NULL,
  WebhookPayload     NVARCHAR(MAX) NULL,
  CreatedAt          DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
  PaidAt             DATETIME2 NULL
);
CREATE INDEX IX_Payments_UserId_Status ON Payments(UserId, Status);
```

---

## 12. Môi trường local Docker (đã có)

```bash
# Build & chạy toàn bộ stack (API + SQL + MQTT)
docker-compose up -d --build

# Xem log API
docker logs smartmarketbot-api -f
```
