# SmartMarketBot — Backend (.NET 10)

Backend cho hệ thống **siêu thị thông minh** (robot dẫn đường + quảng cáo tài trợ + cá nhân hoá hội viên).

- **Stack:** ASP.NET Core 10, Clean Architecture 4-layer, EF Core 10, SQL Server, SignalR, MQTTnet, JWT
- **Schema:** 37 bảng SNAKE_CASE — xem [`db/erd_database.sql`](db/erd_database.sql)
- **Tích hợp:** AI Face Login + Vision (qua repo [`SuperMarketBot-AI`](../SuperMarketBot-AI)), Android, ESP32 IoT

---

## 🚀 Quick Start (Docker)

```bash
# Yêu cầu: Docker Desktop, .NET 10 SDK
docker compose up -d --build

# Sau ~30s, kiểm tra:
curl http://localhost:5000/api/health
# Mở Swagger: http://localhost:5000/swagger
```

| Service        | Port | URL                                          |
| -------------- | ---- | -------------------------------------------- |
| API + Swagger  | 5000 | http://localhost:5000                        |
| SQL Server     | 1433 | `supermarketbot.database.windows.net,1433` (Azure SQL Database) |
| MQTT Broker    | 8883 | `60922debd474446a84747b871c4a8182.s1.eu.hivemq.cloud` (HiveMQ Cloud TLS) |
| SignalR Hub    | 5000 | `ws://localhost:5000/hubs/robot`             |

> AI Face Service (Python) chạy riêng ở repo [`SuperMarketBot-AI`](../SuperMarketBot-AI).

---

## 🛠️ Dev (không dùng Docker)

```bash
# Cấu hình kết nối DB và MQTT đã được trỏ sang Azure SQL và HiveMQ Cloud trong appsettings.json.
# Bạn chỉ cần chạy ứng dụng:
dotnet run --project src/SmartMarketBot.API
```

---

## 📁 Cấu trúc Solution

```
SuperMarketBot-BE/
├── src/
│   ├── SmartMarketBot.Domain/         Entities, enums (no deps)
│   ├── SmartMarketBot.Application/    Interfaces, DTOs, services, Dijkstra
│   ├── SmartMarketBot.Infrastructure/ EF Core, MQTT, JWT, AI proxy
│   └── SmartMarketBot.API/            Controllers, Hubs, Middleware
├── db/
│   └── erd_database.sql               Full 37-table schema (one-shot)
├── scripts/
│   └── gen-hash/                      PBKDF2 hash generator
├── docs/                              Tài liệu dự án
│   ├── architecture/                  Audit, design notes
│   ├── deployment/                    Deploy guides
│   ├── development/                   Dev workflow
│   └── archive/                       Files cũ (lịch sử)
├── .cursor/rules/                     AI coding rules
├── docker-compose.yml                 SQL + Mosquitto + API
├── Dockerfile                         Multi-stage build
├── mosquitto.conf                     MQTT broker config
├── SmartMarketBot.sln
├── README.md
├── AGENTS.md                          AI assistant guide
└── LICENSE
```

---

## 🏗️ Kiến trúc 4 Layer

```
[API Controllers, SignalR Hubs, Middleware]
            │
            ▼
[Application: DTOs, Interfaces, Services, Dijkstra]
            │
            ▼
[Infrastructure: AppDbContext, MQTT, JWT, AI proxy]
            │
            ▼
[Domain: Entities, Enums, Exceptions]
```

**Nguyên tắc:** Domain chỉ chứa POCO, không phụ thuộc EF Core / HTTP. Mọi quan hệ FK đều khai báo tường minh trong `AppDbContext.OnModelCreating`.

---

## 🔐 Auth + Phân quyền

- JWT access token (15 phút) + refresh token (7 ngày, lưu bảng `USER_TOKEN`)
- OTP email 6 số (bảng `EMAIL_OTP`) cho đăng ký / quên mật khẩu
- PBKDF2-SHA256 hashing

Tài khoản demo (sau khi seed `seed_combined.sql`):

| Email                          | Password  | Role   | FullName          |
| ------------------------------ | --------- | ------ | ----------------- |
| `admin@smartmarket.local`      | `123456`  | Admin  | System Admin      |
| `admin2@smartmarket.local`     | `123456`  | Admin  | Võ Hoàng Nam      |
| `staff@smartmarket.local`      | `123456`  | Staff  | Nguyễn Văn Khoa   |
| `staff2@smartmarket.local`     | `123456`  | Staff  | Phạm Thị Dung     |
| `member1@smartmarket.local`    | `123456`  | Member | Nguyễn Văn A      |
| `member2@smartmarket.local`    | `123456`  | Member | Trần Thị Bình     |
| `member3@smartmarket.local`    | `123456`  | Member | Lê Minh Cường     |

> ⚠️ **Lưu ý**: Seed file gốc chứa hash cố định (fake). Sau khi seed, cần chạy script reset password hoặc dùng `scripts/gen-hash` để tạo hash hợp lệ và UPDATE lên DB.

---

## 🔌 Tích hợp với repo khác

| Repo                          | Giao tiếp                                  |
| ----------------------------- | ------------------------------------------ |
| [`SuperMarketBot-AI`](../SuperMarketBot-AI) | HTTP REST (`/verify`, `/extract-vector`) |
| [`SuperMarketBot-FE`](../SuperMarketBot-FE) | REST + SignalR `/hubs/robot`              |
| [`SuperMarketBot-Android`](../SuperMarketBot-Android) | REST + SignalR                     |
| [`SuperMarketBot-Android-Robot`](../SuperMarketBot-Android-Robot) | MQTT pub/sub                    |
| [`SuperMarketBot-IOT`](../SuperMarketBot-IOT) | MQTT publish telemetry → `smartmarketbot/robot/+/telemetry` |

---

## 📚 Tài liệu

- [`docs/architecture/audit_report.md`](docs/architecture/audit_report.md) — Audit code ↔ schema
- [`db/erd_database.sql`](db/erd_database.sql) — Full schema (37 bảng + seed)
- [`.cursor/rules/`](.cursor/rules/) — Quy tắc coding cho AI

---

## 📜 License

Xem [`LICENSE`](LICENSE).
