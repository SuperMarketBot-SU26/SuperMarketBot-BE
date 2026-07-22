# Hướng Dẫn Test Toàn Bộ Luồng Quảng Cáo

> **Phiên bản:** Phase C — Hỗ trợ 3 luồng targeting độc lập (Route + Zone + Shelf)
> **Base URL:** `http://localhost:5000` (theo `docker-compose.yml`)
> **Swagger UI:** `http://localhost:5000/swagger`

---

## 0. Chuẩn Bị Trước Khi Test

### 0.1. Migrations (bắt buộc)

```bash
# 1. Thêm cột IsSystemBrand vào bảng BRAND (nếu chưa có)
sqlcmd -S <server> -d SuperMarketBot -Q "ALTER TABLE dbo.BRAND ADD IsSystemBrand BIT NOT NULL DEFAULT 0;"

# 2. Seed SmartMart brand (tùy chọn)
sqlcmd -S <server> -d SuperMarketBot -Q "IF NOT EXISTS (SELECT 1 FROM BRAND WHERE BrandName = 'SmartMart') BEGIN INSERT INTO BRAND (BrandName, Wallet, IsSystemBrand, CreatedAt) VALUES ('SmartMart', 10000000, 1, GETUTCDATE()); END"

# 3. Chạy migration chính
sqlcmd -S <server> -d SuperMarketBot -i db/migrations/migration_all_in_one.sql
```

File này đã idempotent — chạy nhiều lần không lỗi.

### 0.2. Khởi động dịch vụ

```bash
cd SuperMarketBot-BE
dotnet run --project src/SmartMarketBot.API
# hoặc
docker compose up -d --build
```

### 0.3. Test data cần có sẵn

| Bảng | Dữ liệu tối thiểu | Vai trò trong test |
|------|--------------------|-------------------|
| `MEMBER` | ≥ 1 record | Tương tác xem quảng cáo |
| `BRAND` | ≥ 1 record với `Wallet >= 1000000` | Trừ tiền khi Activate |
| `BRAND` | ≥ 1 record với `IsSystemBrand = 1` (SmartMart) | SmartMart hiển thị ưu tiên trong ads |
| `AD_PACKAGE` | 3 records (đã seed) | Gói giá |
| `ROBOT` | ≥ 1 record | Robot chạy quảng cáo |
| `ZONE`, `AISLE`, `SHELF`, `SLOT` | ≥ 1 record mỗi loại | Targeting Zone |
| `SEMANTIC_OBJECT` | ≥ 1 record (`XMin<=X<=XMax`, `YMin<=Y<=YMax`) | Targeting Shelf + tọa độ |
| `ROBOT_ROUTE` | ≥ 1 record | Targeting Route |

### 0.4. Lấy ID quan trọng trước khi test

```sql
-- Lấy ID để dùng cho test
SELECT PackageID, PackageName, PricePackage, PriceRoute, PriceZone, PriceShelf FROM AD_PACKAGE;
SELECT BrandID, BrandName, Wallet, IsSystemBrand FROM BRAND;
SELECT RobotID, RobotCode FROM ROBOT;
SELECT ZoneID, ZoneName FROM ZONE;
SELECT ObjectID, ObjectType, XMin, XMax, YMin, YMax FROM SEMANTIC_OBJECT;
SELECT RobotRouteID, RouteName FROM ROBOT_ROUTE;
SELECT SlotID FROM SLOT;
SELECT ProductID, ProductName FROM PRODUCT;
```

---

## 1. Tổng Quan API

**6 nhóm × 36 endpoints** (tổng luồng Ads):

| # | Controller | Route prefix | Endpoints | Mục đích |
|---|------------|-------------|-----------|----------|
| 1 | `AdPackagesController` | `/api/v1/ad-packages` | 5 | Quản lý gói giá |
| 2 | `AdResourcesController` | `/api/v1/ad-resources` | 6 | Upload & quản lý media (image/video/voice) |
| 3 | `AdRoutesController` | `/api/v1/ad-routes` | 7 | CRUD route + gán cho robot |
| 4 | `AdCampaignsController` | `/api/v1/ad-campaigns` | 11 | CRUD campaign + Activate/Pause/Cancel + gán route + xem logs |
| 5 | `AdCampaignController` | `/api/v1/ad-campaign` | 6 | Robot playlist (legacy/FE consume) |
| 6 | `RobotImpressionController` | `/api/robots` | 1 | **Ghi impression khi robot đi qua** — 3 luồng UNION |
| 7 | `SponsoredProductsController` | `/api/v1/sponsored-products` | 7 | CRUD sponsored product (Add/Bulk/Priority/Status) |
| 8 | `MemberSponsoredController` | `/api/members` | 1 | Member app gọi lấy recommended ads + allergy check |
| 9 | `MobileProductsController` | `/api/v1/products` | **3** (search + **deals**) | Mobile: search sản phẩm + **General Deals** |

**Tổng cộng: 9 controllers × 47 endpoints** (đã đủ 100% theo code Backend).

---

## 1A. Bảng Quyền Truy Cập (Auth Matrix)

> **Quan trọng cho FE:** Mọi API quảng cáo đều `[AllowAnonymous]` — **KHÔNG cần gửi JWT**.

| Endpoint group | Auth | Lý do thiết kế |
|----------------|------|----------------|
| `/api/v1/ad-packages/**` | AllowAnonymous | Public — brand xem giá trước khi đăng ký |
| `/api/v1/ad-resources/**` | AllowAnonymous | Cho phép upload từ admin app chưa login |
| `/api/v1/ad-routes/**` | AllowAnonymous | Admin route + robot app cùng dùng |
| `/api/v1/ad-campaigns/**` | AllowAnonymous | Admin brand quản lý campaign (chưa có SSO) |
| `/api/v1/ad-campaign/**` (legacy playlist) | AllowAnonymous | Robot app gọi khi chạy |
| `/api/robots/{code}/impression` | AllowAnonymous | Robot gọi khi đi qua slot |
| `/api/v1/sponsored-products/**` | AllowAnonymous | Admin brand quản lý sản phẩm trong campaign |
| `/api/members/{id}/sponsored-recommendations` | AllowAnonymous | Member app hiển thị quảng cáo + allergy check |

**Test JWT (nếu cần):**
```bash
# Một số API khác của hệ thống có [Authorize]
# Lấy token mẫu:
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin@123"}'
# → { "token": "eyJ..." }

# Nhưng KHÔNG cần dùng cho 36 API ads.
```

**Lưu ý cho FE:**
- Nếu app của bạn đã có sẵn token trong localStorage → **KHÔNG cần gửi** cho các API ads (sẽ bị ignore).
- Nếu app muốn **kiểm tra quyền admin** ở phía BE trong tương lai → BE sẽ thông báo cập nhật sau.

---

## 1B. Sơ Đồ Luồng (Flow Diagrams)

### 1B.1. Vòng đời Campaign

```
                    ┌──────────────┐
                    │   Inactive   │ ← Khởi tạo từ POST /ad-campaigns
                    └──────┬───────┘
                           │
                           │ POST /activate (charge wallet)
                           │ validate: routeCount + zoneCount + hasShelf >= 1
                           ▼
                    ┌──────────────┐
              ┌────▶│    Active    │◀────┐
              │     └──────┬───────┘     │
              │            │             │
              │            │ POST /pause │ POST /activate (re-resume)
              │            ▼             │
              │     ┌──────────────┐    │
              │     │    Paused    │────┘
              │     └──────┬───────┘
              │            │
              │            │ POST /cancel (refund)
              │            ▼
              │     ┌──────────────┐
              │     │   Canceled   │
              │     └──────────────┘
              │
              │ (Cron job hàng ngày)
              │ EndDate < now → status = Completed (auto)
              │
              ▼
        ┌──────────────┐
        │  Completed   │
        └──────────────┘
```

### 1B.2. Luồng Activate (Charge Wallet)

```
Brand bấm "Activate Campaign"
        │
        ▼
┌─────────────────────────────────────┐
│ Validate campaign targeting         │
│ - routeCount + zoneCount + hasShelf │
│   >= 1                              │
│ - status hiện tại phải Inactive    │
└─────────────┬───────────────────────┘
              │ OK
              ▼
┌─────────────────────────────────────┐
│ Tính totalCost:                     │
│  PricePackage                       │
│ + PriceRoute × count(routes)        │
│ + PriceZone  × count(zones)         │
│ + PriceShelf × (hasShelf ? 1 : 0)   │
└─────────────┬───────────────────────┘
              │
              ▼
┌─────────────────────────────────────┐
│ Check BRAND.Wallet >= totalCost     │
└─────────────┬───────────────────────┘
              │ Đủ tiền
              ▼
┌─────────────────────────────────────┐
│ Trong transaction:                  │
│  1. INSERT AD_CAMPAIGN_ROUTE        │  ← Snap giá từ PriceRoute
│  2. INSERT AD_CAMPAIGN_ZONE         │  ← Snap giá từ PriceZone
│  3. UPDATE AD_CAMPAIGN              │  ← Snap ShelfPriceCharged
│  4. UPDATE BRAND SET Wallet -= X    │
│  5. UPDATE AD_CAMPAIGN.Status=Active│
│  6. INSERT AD_CAMPAIGN_LOG          │
└─────────────┬───────────────────────┘
              │
              ▼
Response: {
  adCampaignId, campaignName,
  previousStatus: "Inactive",
  newStatus: "Active",
  amountCharged,
  remainingWalletBalance
}
```

### 1B.3. Luồng Impression (Robot → BE)

```
Robot đi tới Slot tọa độ (x, y)
        │
        ▼
POST /api/robots/{robotCode}/impression
Body: { slotId, xCoord, yCoord, memberId? }
        │
        ▼
┌─────────────────────────────────────┐
│ 1. Tìm Robot theo robotCode         │
│ 2. Tìm RobotRoute đang Active      │ (ROUTE_ASSIGNMENT.Status='Active')
│ 3. Tìm SemanticObject chứa (x,y)   │ (xMin≤x≤xMax && yMin≤y≤yMax)
│ 4. Tìm Zone của Slot                │ (SLOT.ZoneID)
└─────────────┬───────────────────────┘
              │
              ▼
┌─────────────────────────────────────┐
│ UNION 3 luồng — tìm campaign match: │
│                                     │
│ A. Route hit:                       │
│    SELECT FROM AD_CAMPAIGN_ROUTE    │
│    WHERE RobotRouteID = routeId     │
│                                     │
│ B. Zone hit:                        │
│    SELECT FROM AD_CAMPAIGN_ZONE     │
│    WHERE ZoneID = zoneId            │
│                                     │
│ C. Shelf hit:                       │
│    SELECT FROM AD_CAMPAIGN          │
│    WHERE SemanticObjectID = objId   │
└─────────────┬───────────────────────┘
              │
              ▼
┌─────────────────────────────────────┐
│ Cho mỗi SponsoredProduct:           │
│  - 1 nếu match route → charge       │
│    RoutePriceCharged                │
│  - 1 nếu match zone → charge        │
│    ZonePriceCharged                 │
│  - 1 nếu match shelf → charge       │
│    ShelfPriceCharged                │
│                                     │
│ Charge = MAX(route, zone, shelf)    │
│ KHÔNG cộng dồn                      │
└─────────────┬───────────────────────┘
              │
              ▼
INSERT AD_CAMPAIGN_LOG (ActionType='RoutePass')
Response: { impressionCount, totalChargedAmount, logs[], semanticObjectId }
```

---

## 1C. Response Mẫu Đầy Đủ (Cho FE)

### 1C.1. `GET /api/v1/ad-packages`

```json
[
  {
    "packageId": 1,
    "packageName": "Gói Cơ Bản",
    "pricePackage": 1000000.00,
    "priceRoute": 200000.00,
    "priceZone": 30000.00,
    "priceShelf": 15000.00,
    "basePriceClick": 5000.00,
    "adScore": 50,
    "status": "Active"
  },
  ...
]
```

### 1C.2. `POST /api/v1/ad-campaigns/with-products` → 201 Created

```json
{
  "adCampaignId": 10,
  "campaignName": "Coca mùa hè 2026",
  "packageId": 1,
  "packageName": "Gói Cơ Bản",
  "brandId": 1,
  "brandName": "Coca-Cola",
  "semanticObjectId": 5,
  "startDate": "2026-07-01T00:00:00Z",
  "endDate": "2027-01-01T00:00:00Z",
  "status": "Inactive",
  "sponsoredProductCount": 3,
  "totalSpent": 0,
  "routeIds": [1]
}
```

### 1C.3. `POST /api/v1/ad-campaigns/{id}/activate` → 200 OK

```json
{
  "adCampaignId": 10,
  "campaignName": "Coca mùa hè 2026",
  "previousStatus": "Inactive",
  "newStatus": "Active",
  "amountCharged": 1230000.00,
  "remainingWalletBalance": 8770000.00
}
```

### 1C.4. `GET /api/v1/ad-campaigns/{id}/routes`

```json
{
  "adCampaignId": 10,
  "brandId": 1,
  "routeCount": 2,
  "totalRouteCharge": 400000.00,
  "routes": [
    { "robotRouteId": 1, "routeName": "Tuyến 1", "routePriceCharged": 200000.00, "purchasedAt": "2026-07-11T08:00:00Z" },
    { "robotRouteId": 2, "routeName": "Tuyến 2", "routePriceCharged": 200000.00, "purchasedAt": "2026-07-11T08:00:00Z" }
  ]
}
```

### 1C.5. `GET /api/v1/ad-campaign/{robotId}/robot-playlist?semanticObjectId=5`

```json
{
  "robotId": 1,
  "currentZoneId": 2,
  "semanticObjectId": 5,
  "generatedAt": "2026-07-11T09:00:00Z",
  "playlist": [
    {
      "sponsoredId": 1,
      "adCampaignId": 10,
      "campaignName": "Coca mùa hè 2026",
      "productId": 101,
      "productName": "Coca-Cola 330ml",
      "productPrice": 12000.00,
      "priority": 100,
      "adScore": 50,
      "endDate": "2027-01-01T00:00:00Z",
      "imageUrl": "https://cdn.example.com/ad/abc.jpg",
      "displayDurationSeconds": 8,
      "mediaContents": [
        { "resourceType": "IMAGE", "resourceUrl": "https://...", "contentText": null, "resolution": "1920x1080" },
        { "resourceType": "VOICE_TEXT", "resourceUrl": null, "contentText": "Coca — Taste the Feeling!", "resolution": null }
      ]
    }
  ]
}
```

### 1C.6. `POST /api/robots/{code}/impression` → 200 OK ⭐

```json
{
  "robotCode": "ROBOT01",
  "slotId": 10,
  "semanticObjectId": 5,
  "impressionCount": 1,
  "totalChargedAmount": 200000.00,
  "message": "Recorded 1 impressions for campaign 10 (route hit)",
  "logs": [
    {
      "adCampaignId": 10,
      "sponsoredId": 1,
      "productId": 101,
      "productName": "Coca-Cola 330ml",
      "matchType": "Route",          // "Route" | "Zone" | "Shelf" | "Route+Zone" | ...
      "chargedAmount": 200000.00,
      "chargedAt": "2026-07-11T09:01:23Z"
    }
  ]
}
```

### 1C.7. `GET /api/v1/ad-campaigns/{id}/logs?pageNumber=1&pageSize=20`

```json
{
  "items": [
    {
      "logId": 1001,
      "adCampaignId": 10,
      "campaignName": "Coca mùa hè 2026",
      "actionType": "RoutePass",
      "chargedAmount": 200000.00,
      "timestamp": "2026-07-11T09:01:23Z",
      "sponsoredId": 1,
      "productId": 101,
      "productName": "Coca-Cola 330ml",
      "robotId": 1,
      "zoneId": 2,
      "memberId": 5,
      "sessionId": "abc-xyz-123",
      "isFraud": false
    },
    {
      "logId": 999,
      "adCampaignId": 10,
      "actionType": "Activation",
      "chargedAmount": 1230000.00,
      "timestamp": "2026-07-11T08:00:00Z",
      "isFraud": false
    }
  ],
  "totalCount": 47,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 3
}
```

### 1C.8. `POST /api/v1/ad-campaign/log-interaction`

**Body:**
```json
{
  "adCampaignId": 10,
  "actionType": "Click",  // Click | Navigation | Impression
  "sponsoredId": 1,
  "productId": 101,
  "robotId": 1,
  "semanticObjectId": 5,
  "zoneId": 2,
  "slotId": 10,
  "memberId": 5,
  "sessionId": "abc-xyz-123",
  "xCoord": 150,
  "yCoord": 200
}
```

**Response:**
```json
{
  "success": true,
  "logId": 1002,
  "chargedAmount": 5000.00,
  "isFraud": false,
  "fraudReason": null,
  "message": "Click recorded successfully."
}
```

---

## 2. Chi Tiết Từng API + Cách Test

### 2.1. Quản Lý Package — `/api/v1/ad-packages`

#### 2.1.1. `GET /api/v1/ad-packages` — Lấy danh sách gói

**Test:**
```bash
curl -X GET http://localhost:5000/api/v1/ad-packages
```

**Mong đợi:** Trả về 3 packages mặc định (Cơ Bản, Tiêu Chuẩn, Cao Cấp). Verify `PriceZone`, `PriceShelf` đã có giá trị sau migration.

---

#### 2.1.2. `GET /api/v1/ad-packages/{packageId}` — Chi tiết 1 gói

**Test:**
```bash
curl -X GET http://localhost:5000/api/v1/ad-packages/1
```

**Mong đợi:** `200 OK` + JSON của package. **404** nếu ID không tồn tại.

---

#### 2.1.3. `POST /api/v1/ad-packages` — Tạo package mới

**Body:**
```json
{
  "packageName": "Gói Test",
  "pricePackage": 500000,
  "priceRoute": 100000,
  "priceZone": 30000,
  "priceShelf": 15000,
  "basePriceClick": 5000,
  "adScore": 50,
  "status": "Active"
}
```

**Test:**
```bash
curl -X POST http://localhost:5000/api/v1/ad-packages \
  -H "Content-Type: application/json" \
  -d @body.json
```

**Mong đợi:** `201 Created` + JSON với `packageId` mới.

---

#### 2.1.4. `PUT /api/v1/ad-packages/{packageId}` — Cập nhật

**Body:** (giống POST, không bắt buộc PackageName)

**Test:**
```bash
curl -X PUT http://localhost:5000/api/v1/ad-packages/1 \
  -H "Content-Type: application/json" \
  -d '{"pricePackage": 999999, "priceRoute": 100000, "priceZone": 30000, "priceShelf": 15000}'
```

**Mong đợi:** `200 OK` + package đã update.

---

#### 2.1.5. `DELETE /api/v1/ad-packages/{packageId}` — Xóa

**Test:**
```bash
curl -X DELETE http://localhost:5000/api/v1/ad-packages/4
```

**Mong đợi:** `204 No Content`. Lỗi `400` nếu còn campaign đang dùng.

---

### 2.2. Quản Lý Resource — `/api/v1/ad-resources`

#### 2.2.1. `GET /api/v1/ad-resources/campaign/{campaignId}` — Lấy media của campaign

**Test:**
```bash
curl -X GET "http://localhost:5000/api/v1/ad-resources/campaign/1?pageNumber=1&pageSize=20"
```

**Mong đợi:** Phân trang. **200 OK** + `items[]`.

---

#### 2.2.2. `GET /api/v1/ad-resources/{resourceId}` — Chi tiết 1 resource

**Test:**
```bash
curl -X GET http://localhost:5000/api/v1/ad-resources/1
```

---

#### 2.2.3. `POST /api/v1/ad-resources/upload` — Upload file

**Test (form-data):**
```bash
curl -X POST http://localhost:5000/api/v1/ad-resources/upload \
  -F "AdCampaignId=1" \
  -F "ResourceType=IMAGE" \
  -F "File=@/path/to/image.jpg" \
  -F "Resolution=1920x1080"
```

**Mong đợi:** `201 Created` + `resourceUrl`.

---

#### 2.2.4. `POST /api/v1/ad-resources` — Tạo từ URL (không upload)

**Body:**
```json
{
  "adCampaignId": 1,
  "resourceType": "VOICE_TEXT",
  "resourceUrl": null,
  "contentText": "Coca Cola — Taste the Feeling!",
  "resolution": null
}
```

---

#### 2.2.5. `PATCH /api/v1/ad-resources/{resourceId}/status` — Đổi trạng thái

**Body:** `{"status": "Inactive"}`

---

#### 2.2.6. `DELETE /api/v1/ad-resources/{resourceId}` — Xóa

---

### 2.3. Quản Lý Route — `/api/v1/ad-routes`

#### 2.3.1. `GET /api/v1/ad-routes?pageNumber=1&pageSize=10&isActive=true`

---

#### 2.3.2. `GET /api/v1/ad-routes/{routeId}`

---

#### 2.3.3. `GET /api/v1/ad-routes/robot/{robotId}/active` — Route đang active của robot

**Quan trọng cho test:** Trả về route mà robot đang chạy.

```bash
curl -X GET http://localhost:5000/api/v1/ad-routes/robot/1/active
```

---

#### 2.3.4. `POST /api/v1/ad-routes` — Tạo route mới

**Body:**
```json
{
  "routeName": "Tuyến chính tầng 1",
  "mapId": 1,
  "isActive": true,
  "description": "Tuyến test 3 luồng"
}
```

---

#### 2.3.5. `PUT /api/v1/ad-routes/{routeId}` — Cập nhật

---

#### 2.3.6. `DELETE /api/v1/ad-routes/{routeId}` — Xóa

---

#### 2.3.7. `POST /api/v1/ad-routes/{routeId}/assign/{robotId}` — Gán cho robot

```bash
curl -X POST http://localhost:5000/api/v1/ad-routes/1/assign/1
```

**Quan trọng cho test 3 luồng:** Phải gán trước khi test `record-impression` để robot có route Active.

---

### 2.4. Quản Lý Campaign — `/api/v1/ad-campaigns` (CRUD chính)

#### 2.4.1. `GET /api/v1/ad-campaigns?pageNumber=1&pageSize=10&status=Active&brandId=1&searchTerm=Coca`

Phân trang + filter.

---

#### 2.4.2. `GET /api/v1/ad-campaigns/{campaignId}` — Chi tiết

---

#### 2.4.3. `POST /api/v1/ad-campaigns` — Tạo campaign (không có product)

**Body — Test 1: Targeting theo Route (1 route)**
```json
{
  "packageId": 1,
  "brandId": 1,
  "semanticObjectId": null,
  "zoneIds": null,
  "routeIds": [1],
  "campaignName": "Test chỉ có Route",
  "startDate": "2026-07-01T00:00:00Z",
  "endDate": "2026-08-01T00:00:00Z"
}
```

**Body — Test 2: Targeting theo Zone (không route, không shelf)**
```json
{
  "packageId": 1,
  "brandId": 1,
  "semanticObjectId": null,
  "zoneIds": [1, 2],
  "routeIds": null,
  "campaignName": "Test chỉ có Zone",
  "startDate": "...",
  "endDate": "..."
}
```

**Body — Test 3: Targeting theo Shelf (kệ cụ thể)**
```json
{
  "packageId": 1,
  "brandId": 1,
  "semanticObjectId": 5,
  "zoneIds": null,
  "routeIds": null,
  "campaignName": "Test chỉ có Shelf",
  ...
}
```

**Body — Test 4: Targeting cả 3 luồng (kết hợp)**
```json
{
  "packageId": 1,
  "brandId": 1,
  "semanticObjectId": 5,
  "zoneIds": [1, 2],
  "routeIds": [1],
  "campaignName": "Test cả 3 luồng"
}
```

**Mong đợi:** `201 Created` + campaign mới ở trạng thái `Inactive`.

---

#### 2.4.4. `POST /api/v1/ad-campaigns/with-products` — Tạo campaign có luôn sản phẩm

**Body:**
```json
{
  "packageId": 1,
  "brandId": 1,
  "semanticObjectId": 5,
  "zoneIds": [1, 2],
  "routeIds": [1, 2],
  "campaignName": "Campaign đầy đủ route + zone + shelf + products",
  "startDate": "2026-07-01T00:00:00Z",
  "endDate": "2026-09-01T00:00:00Z",
  "productIds": [101, 102, 103]
}
```

**Mong đợi:** `201 Created` + campaign kèm `sponsoredProducts`.

---

#### 2.4.5. `PUT /api/v1/ad-campaigns/{campaignId}` — Cập nhật (chỉ khi Inactive)

**Body:** Tương tự POST, có thể đổi `RouteIds`, `ZoneIds`, `SemanticObjectId`.

**Quan trọng:** Update route/zone/shelf = **snapshot giá mới** (charge lại giá tại lúc update).

---

#### 2.4.6. `DELETE /api/v1/ad-campaigns/{campaignId}` — Xóa

**Chỉ xóa được khi `Inactive`.** Lỗi nếu đang `Active`.

---

#### 2.4.7. `POST /api/v1/ad-campaigns/{campaignId}/activate` ⭐

**⭐ QUAN TRỌNG NHẤT — charge wallet theo 3 luồng:**

```bash
curl -X POST http://localhost:5000/api/v1/ad-campaigns/1/activate
```

**Công thức charge:**
```
totalCost = PricePackage
          + PriceRoute × count(AdCampaignRoute)
          + PriceZone  × count(AdCampaignZone)
          + PriceShelf × (SemanticObjectId.HasValue ? 1 : 0)

Điều kiện: routeCount + zoneCount + hasShelf >= 1
```

**Mong đợi:**
- `200 OK` + JSON chứa `amountCharged`, `remainingWalletBalance`.
- `400 Bad Request` nếu không có targeting nào.
- `400` nếu wallet không đủ tiền.

**Verify DB sau khi activate:**
```sql
-- Wallet đã trừ
SELECT Wallet FROM BRAND WHERE BrandID = 1;

-- AD_CAMPAIGN_LOG có 1 entry "Activation"
SELECT * FROM AD_CAMPAIGN_LOG WHERE AdCampaignID = 1 AND ActionType = 'Activation';

-- AdCampaign.Status = 'Active'
SELECT Status FROM AD_CAMPAIGN WHERE AdCampaignID = 1;
```

---

#### 2.4.8. `POST /api/v1/ad-campaigns/{campaignId}/pause` — Tạm dừng

**Body:** `{"reason": "Test pause"}`

**Mong đợi:** Status → `Paused`. Không charge thêm.

---

#### 2.4.9. `POST /api/v1/ad-campaigns/{campaignId}/cancel` — Hủy

**Mong đợi:** Status → `Canceled`, **refund** một phần.

---

#### 2.4.10. `GET /api/v1/ad-campaigns/{campaignId}/logs` — Xem logs

```bash
curl -X GET "http://localhost:5000/api/v1/ad-campaigns/1/logs?pageNumber=1&pageSize=20"
```

**Mong đợi:** Phân trang logs (RoutePass / Click / Activation / Pause / ...).

---

#### 2.4.11. `POST /api/v1/ad-campaigns/{campaignId}/routes` ⭐ — Gán route sau

**⭐ Test 3 luồng:** Gán thêm route mà không cần tạo lại campaign.

**Body:**
```json
{ "routeIds": [1, 2, 3] }
```

**Test:**
```bash
curl -X POST http://localhost:5000/api/v1/ad-campaigns/1/routes \
  -H "Content-Type: application/json" \
  -d '{ "routeIds": [1, 2, 3] }'
```

**Chỉ thao tác được khi Inactive / Paused.** Đã mua route rồi thì update snapshot giá.

---

#### 2.4.12. `GET /api/v1/ad-campaigns/{campaignId}/routes` — Xem routes đã mua

```bash
curl -X GET http://localhost:5000/api/v1/ad-campaigns/1/routes
```

**Mong đợi:** JSON với `routeCount`, `totalRouteCharge`, `routes[]` (mỗi route kèm `routePriceCharged`).

---

### 2.5. Robot Playlist (Legacy FE consume) — `/api/v1/ad-campaign`

> Mục đích: FE app gọi khi robot dừng trước 1 kệ/zonelfNode để lấy danh sách ad cần phát. **Không charge** ở các API này (chỉ trả playlist).

#### 2.5.1. `GET /api/v1/ad-campaign/robot-playlist/{robotId}?semanticObjectId=5`

Trả playlist cho robot tại kệ cụ thể.

---

#### 2.5.2. `GET /api/v1/ad-campaign/robot-playlist/{robotId}/zone/{zoneId}`

Trả playlist cho robot trong zone.

---

#### 2.5.3. `GET /api/v1/ad-campaign/robot-playlist/{robotId}/autonomous`

Robot tự chạy (ad-hoc) — lấy playlist theo route hiện tại.

---

#### 2.5.4. `GET /api/v1/ad-campaign/robot-playlist/{robotId}/node/{nodeId}`

Playlist khi robot đang ở node navigation cụ thể.

---

#### 2.5.5. `POST /api/v1/ad-campaign/log-interaction`

Ghi log tương tác (Click / Navigation / Impression).

**Body:**
```json
{
  "adCampaignId": 1,
  "actionType": "Click",
  "sponsoredId": 1,
  "productId": 101,
  "robotId": 1,
  "memberId": 5,
  "sessionId": "abc-xyz-123"
}
```

---

#### 2.5.6. `PUT /api/v1/ad-campaign/session/bind`

**Bind session ẩn danh → Member chính thức.**

**Body:**
```json
{ "sessionId": "abc-xyz-123", "memberId": 5 }
```

---

### 2.6. Robot Impression (3-luồng) — `/api/robots`

> **⭐ Endpoint quan trọng nhất — chạy 3-luồng UNION.**

#### 2.6.1. `POST /api/robots/{robotCode}/impression` ⭐⭐⭐

**Robot báo vừa tới SlotId tại tọa độ (x,y).** BE tự tìm route/zone/shelf → UNION 3 luồng → ghi impression.

**Body:**
```json
{
  "slotId": 10,
  "xCoord": 150,
  "yCoord": 200,
  "memberId": 5
}
```

**Test (happy path — Route trúng):**
```bash
curl -X POST http://localhost:5000/api/robots/ROBOT01/impression \
  -H "Content-Type: application/json" \
  -d '{
    "slotId": 10,
    "xCoord": 150,
    "yCoord": 200,
    "memberId": 5
  }'
```

**Mong đợi:**
- `200 OK` + JSON chứa:
  - `impressionCount > 0`
  - `totalChargedAmount` = tổng snapshot giá (Route/Zone/Shelf, MAX nếu trùng)
  - `logs[]` — mỗi SponsoredProduct đã phát
  - `semanticObjectId` = ObjectID chứa (x,y)

**Verify DB sau khi ghi impression:**
```sql
-- Log entry với ActionType='RoutePass'
SELECT *
FROM AD_CAMPAIGN_LOG
WHERE RobotID = 1 AND ActionType = 'RoutePass'
ORDER BY Timestamp DESC;

-- Wallet KHÔNG bị trừ ở bước này (charge đã trừ lúc Activate)
SELECT Wallet FROM BRAND WHERE BrandID = 1;
```

---

### 2.7. Quản Lý Sponsored Product — `/api/v1/sponsored-products`

> **Mục đích:** Quản lý sản phẩm gắn vào campaign (Add thêm sau khi tạo, đổi Priority, đổi Status).

#### 2.7.1. `GET /api/v1/sponsored-products/campaign/{campaignId}` — DS Sponsored theo campaign

```bash
curl -X GET http://localhost:5000/api/v1/sponsored-products/campaign/1
```

**Response:**
```json
[
  {
    "sponsoredId": 1,
    "adCampaignId": 10,
    "campaignName": "Coca mùa hè 2026",
    "productId": 101,
    "productName": "Coca-Cola 330ml",
    "productPrice": 12000.00,
    "priority": 50,
    "status": "Active"
  }
]
```

---

#### 2.7.2. `GET /api/v1/sponsored-products/{sponsoredId}` — Chi tiết

---

#### 2.7.3. `POST /api/v1/sponsored-products` — Thêm 1 sản phẩm

**Body:**
```json
{
  "adCampaignId": 10,
  "productId": 102,
  "priority": 50
}
```

**Mong đợi:** `201 Created` + SponsoredProductDto.

---

#### 2.7.4. `POST /api/v1/sponsored-products/bulk` — Thêm nhiều sản phẩm

**Body:**
```json
{
  "adCampaignId": 10,
  "products": [
    { "productId": 102, "priority": 50 },
    { "productId": 103, "priority": 30 }
  ]
}
```

**Mong đợi:** `200 OK` + array `SponsoredProductDto[]`.

---

#### 2.7.5. `PUT /api/v1/sponsored-products/{sponsoredId}/priority` — Đổi Priority

**Body:** `{"priority": 80}`

**Dùng để:** Sort playlist ưu tiên (kết hợp với AdScore + EndDate).

---

#### 2.7.6. `PATCH /api/v1/sponsored-products/{sponsoredId}/status` — Đổi Status

**Body:** `{"status": "Inactive"}`

**Validate:** Chỉ nhận `Active` / `Inactive`.

---

#### 2.7.7. `DELETE /api/v1/sponsored-products/{sponsoredId}` — Xóa sản phẩm khỏi campaign

---

### 2.8. Member Sponsored Recommendations — `/api/members`

> **Mục đích:** Member app (Flutter/React) gọi lấy quảng cáo được cá nhân hóa, có allergy check.

#### 2.8.1. `GET /api/members/{memberId}/sponsored-recommendations?slotId={slotId}` ⭐⭐

**⭐ Quan trọng cho Member App — đã cá nhân hóa + allergy check:**

```bash
curl -X GET "http://localhost:5000/api/members/5/sponsored-recommendations?slotId=10"
```

**Cách hoạt động:**
1. Lấy tất cả campaign Active trong thời gian chạy
2. Lấy SemanticObjectIds từ các campaign → suy ra ProductTypeIds
3. Lấy SponsoredProducts có Product cùng ProductType
4. **Allergy check:** với mỗi SponsoredProduct, kiểm tra Product có HealthTag nào trùng với Member dị ứng → set `HasAllergenConflict=true`
5. Tính `TotalScore = Priority + AdScore + ProfileBase(20) + WeekendBonus(10 if Sat/Sun)`
6. Sort theo `TotalScore DESC, ProductName ASC`

**Response:**
```json
{
  "memberId": 5,
  "contextSlotId": 10,
  "contextZoneId": 2,
  "contextZoneName": "Khu rau củ",
  "totalCount": 8,
  "items": [
    {
      "sponsoredId": 1,
      "adCampaignId": 10,
      "campaignName": "Coca mùa hè 2026",
      "brandId": 1,
      "brandName": "Coca-Cola",
      "productId": 101,
      "productName": "Coca-Cola 330ml",
      "unitPrice": 12000.00,
      "promotionPrice": 10000.00,
      "imageUrl": "https://cdn.example.com/products/coca.jpg",
      "slotId": 10,
      "slotCode": "A1-S05",
      "zoneId": 2,
      "zoneName": "Khu nước giải khát",
      "priority": 50,
      "profileScore": 20,
      "weekendBonus": 10,
      "totalScore": 130,
      "hasAllergenConflict": false,
      "allergenConflicts": []
    },
    {
      "sponsoredId": 2,
      "adCampaignId": 11,
      "campaignName": "Sữa Vinamilk",
      "productId": 201,
      "productName": "Sữa tươi Vinamilk 1L",
      "unitPrice": 35000.00,
      "promotionPrice": null,
      "priority": 30,
      "profileScore": 20,
      "weekendBonus": 0,
      "totalScore": 100,
      "hasAllergenConflict": true,
      "allergenConflicts": ["Sữa", "Lactose"]
    }
  ]
}
```

**FE dùng để:**
- Hiển thị danh sách quảng cáo recommend trên Member App
- Nếu `hasAllergenConflict=true` → hiển thị cảnh báo đỏ "Chứa chất bạn dị ứng"
- Sắp xếp theo `totalScore` (đã sort sẵn)

**Verify DB sau khi gọi:**
```sql
-- Không ghi log — chỉ đọc
-- Lưu ý: dữ liệu đến từ JOIN SponsoredProducts + AdCampaigns + Brands + Products
--         + ProductHealthTags + MemberHealthPreferences + Slots
```

---

### 2.9. General Deals — `GET /api/v1/products/deals`

> **⚡ MỚI — Dành cho Guest (chưa đăng nhập) và Member đã đăng nhập.**
> Trả về tất cả sản phẩm đang giảm giá trên **toàn siêu thị**.
> Nguồn deal: `Product.PromotionPrice != null` + `SponsoredProducts` thuộc `AdCampaign Active`.

#### 2.9.1. `GET /api/v1/products/deals` ⭐⭐⭐

```bash
# Guest (không cần đăng nhập)
curl -X GET "http://localhost:5000/api/v1/products/deals?pageNumber=1&pageSize=20"

# Member đã đăng nhập (có allergy check)
curl -X GET "http://localhost:5000/api/v1/products/deals?memberId=5&pageNumber=1&pageSize=20"

# Lọc theo ProductType
curl -X GET "http://localhost:5000/api/v1/products/deals?productTypeId=3&pageNumber=1&pageSize=10"

# Chỉ deals giảm ≥ 20%
curl -X GET "http://localhost:5000/api/v1/products/deals?minDiscountPercent=20"
```

**Query params:**

| Param | Type | Mô tả |
|-------|------|--------|
| `productTypeId` | int? | Lọc theo loại sản phẩm |
| `categoryId` | int? | Lọc theo danh mục |
| `minDiscountPercent` | int? | Chỉ deals giảm ≥ N% |
| `memberId` | int? | Nếu truyền → check allergy, trả `hasAllergenConflict` |
| `pageNumber` | int | Default 1 |
| `pageSize` | int | Default 20, max 100 |

**Response mẫu:**
```json
{
  "totalCount": 42,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 3,
  "items": [
    {
      "productId": 101,
      "productName": "Coca-Cola 330ml",
      "originalPrice": 12000.00,
      "dealPrice": 10000.00,
      "discountPercent": 17,
      "promotionLabel": "⚡ Khuyến mãi",
      "imageUrl": "https://cdn.example.com/products/coca.jpg",
      "productTypeName": "Nước giải khát",
      "productTypeId": 3,
      "brandName": "Coca-Cola",
      "brandId": 1,
      "healthTags": [],
      "hasAllergenConflict": false,
      "allergenConflicts": [],
      "adCampaignName": "Coca mùa hè 2026",
      "adCampaignId": 10,
      "slotCode": "A1-S05",
      "slotId": 15
    },
    {
      "productId": 201,
      "productName": "Sữa tươi Vinamilk 1L",
      "originalPrice": 35000.00,
      "dealPrice": 25000.00,
      "discountPercent": 29,
      "promotionLabel": "🏷️ Giảm giá",
      "imageUrl": "https://cdn.example.com/products/sua.jpg",
      "productTypeName": "Sữa",
      "productTypeId": 5,
      "brandName": "Vinamilk",
      "brandId": 2,
      "healthTags": ["Sữa", "Lactose"],
      "hasAllergenConflict": false,
      "allergenConflicts": [],
      "adCampaignName": null,
      "adCampaignId": null,
      "slotCode": null,
      "slotId": null
    }
  ]
}
```

**Sort order:** Sponsored deal lên đầu → giảm % cao nhất → tên A-Z.

**FE dùng để:**
- **Guest:** Hiển thị trang Deals/Flash Sale trên app — không cần đăng nhập.
- **Member:** Khi `memberId` truyền → trả kèm `hasAllergenConflict` để hiển thị cảnh báo dị ứng.
- **SmartMart Boost:** Khi `isSystemBrand = true` → hiển thị badge "SmartMart" để phân biệt với brand bên ngoài.

---

### 2.9.2. Test Case — `IsSystemBrand` Boost trong Deals ⭐

#### Seed SmartMart Brand

```sql
-- Kiểm tra SmartMart đã tồn tại chưa
SELECT BrandID, BrandName, Wallet, IsSystemBrand FROM BRAND WHERE BrandName = 'SmartMart';

-- Nếu chưa có, thêm vào
INSERT INTO BRAND (BrandName, Wallet, IsSystemBrand, CreatedAt)
VALUES ('SmartMart', 10000000, 1, GETUTCDATE());
```

#### Test: SmartMart hiển thị ưu tiên trong Sponsored Deals

```bash
# Tạo SmartMart campaign với product deal
curl -X POST http://localhost:5000/api/v1/ad-campaigns \
  -H "Content-Type: application/json" \
  -d '{
    "packageId": 1,
    "brandId": <SmartMartBrandID>,
    "semanticObjectId": null,
    "zoneIds": null,
    "routeIds": null,
    "campaignName": "SmartMart Deal Campaign",
    "startDate": "2026-07-01T00:00:00Z",
    "endDate": "2027-01-01T00:00:00Z"
  }'

# Activate campaign
curl -X POST http://localhost:5000/api/v1/ad-campaigns/<campaignId>/activate

# Thêm product deal cho SmartMart
curl -X POST http://localhost:5000/api/v1/sponsored-products \
  -H "Content-Type: application/json" \
  -d '{
    "adCampaignId": <campaignId>,
    "productId": <productId>,
    "priority": 100
  }'
```

#### Verify Response

```bash
curl -X GET "http://localhost:5000/api/v1/products/deals?pageNumber=1&pageSize=20"
```

**Mong đợi:**
```json
{
  "totalCount": 2,
  "items": [
    {
      "productId": 101,
      "productName": "Sữa SmartMart 1L",
      "dealPrice": 25000.00,
      "discountPercent": 29,
      "brandName": "SmartMart",
      "brandId": 99,
      "isSystemBrand": true,       // ← SmartMart được đánh dấu
      "promotionLabel": "SmartMart Deal"
    },
    {
      "productId": 201,
      "productName": "Coca-Cola 330ml",
      "dealPrice": 10000.00,
      "discountPercent": 17,
      "brandName": "Coca-Cola",
      "brandId": 1,
      "isSystemBrand": false,
      "promotionLabel": "Khuyến mãi"
    }
  ]
}
```

#### Test: Sponsored Recommendations boost SmartMart

```bash
curl -X GET "http://localhost:5000/api/members/5/sponsored-recommendations?slotId=10"
```

**Mong đợi:**
- SmartMart sponsored products được sort lên đầu dựa trên `systemBrandBonus`
- `isSystemBrand: true` xuất hiện trong response để FE hiển thị badge

---

## 3. Kịch Bản Test E2E Đầy Đủ

### 3.1. Chuẩn Bị

```bash
# 1. DB reset + seed
sqlcmd -S <server> -d SuperMarketBot -i db/migrations/migration_all_in_one.sql

# 2. Chạy API
dotnet run --project src/SmartMarketBot.API
```

### 3.2. Kịch Bản — "Test 3 Luồng Targeting"

#### Bước 1: Kiểm tra dữ liệu nền

```bash
# Lấy BrandID, RobotID, RouteID, ZoneID, SemanticObjectID có sẵn
# (xem SQL ở mục 0.4)
```

#### Bước 2: Tạo campaign trỏ vào cả 3 luồng

```bash
curl -X POST http://localhost:5000/api/v1/ad-campaigns/with-products \
  -H "Content-Type: application/json" \
  -d '{
    "packageId": 1,
    "brandId": 1,
    "semanticObjectId": 1,
    "zoneIds": [1],
    "routeIds": [1],
    "campaignName": "E2E Test 3 luồng",
    "startDate": "2026-07-01T00:00:00Z",
    "endDate": "2027-01-01T00:00:00Z",
    "productIds": [101]
  }'
```

→ Trả về `campaignId` (giả sử = `10`).

#### Bước 3: Verify snapshot giá

```sql
SELECT AdCampaignID, SemanticObjectID, ShelfPriceCharged, ShelfPurchasedAt FROM AD_CAMPAIGN WHERE AdCampaignID = 10;
SELECT * FROM AD_CAMPAIGN_ZONE WHERE AdCampaignID = 10;       -- ZonePriceCharged
SELECT * FROM AD_CAMPAIGN_ROUTE WHERE AdCampaignID = 10;       -- RoutePriceCharged
```

**Mong đợi:**
- `ShelfPriceCharged` = `PriceShelf` của package 1
- `ZonePriceCharged` của từng zone = `PriceZone` của package 1
- `RoutePriceCharged` của từng route = `PriceRoute` của package 1

#### Bước 4: Check wallet trước

```sql
SELECT Wallet FROM BRAND WHERE BrandID = 1;
-- Giả sử = 10,000,000 VND
```

#### Bước 5: Activate campaign ⭐

```bash
curl -X POST http://localhost:5000/api/v1/ad-campaigns/10/activate
```

**Mong đợi response mẫu:**
```json
{
  "adCampaignId": 10,
  "campaignName": "E2E Test 3 luồng",
  "previousStatus": "Inactive",
  "newStatus": "Active",
  "amountCharged": 1230000,
  "remainingWalletBalance": 8770000
}
```

**Verify:**
- `amountCharged` = `PricePackage(1M)` + `PriceRoute×1` + `PriceZone×1` + `PriceShelf×1`
- Wallet giảm đúng số `amountCharged`.
- `AD_CAMPAIGN_LOG` có 1 entry `ActionType='Activation'`, `ChargedAmount=amountCharged`.

#### Bước 6: Gán robot cho route

```bash
curl -X POST http://localhost:5000/api/v1/ad-routes/1/assign/1
```

**Verify:**
```sql
SELECT * FROM ROUTE_ASSIGNMENT WHERE RobotID = 1 AND Status = 'Active';
```

#### Bước 7: Robot gửi impression (Route trúng) ⭐

```bash
curl -X POST http://localhost:5000/api/robots/ROBOT01/impression \
  -H "Content-Type: application/json" \
  -d '{ "slotId": 5, "xCoord": 10, "yCoord": 20, "memberId": 1 }'
```

**Mong đợi:**
- `impressionCount > 0` (≥ số SponsoredProduct)
- `totalChargedAmount > 0` (= `RoutePriceCharged` vì route trùng)
- `message` = "Recorded {N} impressions for campaign..."

**Verify:**
```sql
-- Log mới với ActionType='RoutePass'
SELECT LogID, ActionType, ChargedAmount, SponsoredID, ProductID, RobotID,
       SemanticObjectID, ZoneID, SlotID, MemberID
FROM AD_CAMPAIGN_LOG
WHERE AdCampaignID = 10 AND ActionType = 'RoutePass'
ORDER BY Timestamp DESC;
```

#### Bước 8: Test Zone trúng (route khác, nhưng zone trùng)

Tạo campaign 2 chỉ mua zone:
```bash
curl -X POST http://localhost:5000/api/v1/ad-campaigns \
  -H "Content-Type: application/json" \
  -d '{
    "packageId": 1, "brandId": 1,
    "semanticObjectId": null,
    "zoneIds": [1], "routeIds": null,
    "campaignName": "Zone only",
    "startDate": "2026-07-01T00:00:00Z", "endDate": "2027-01-01T00:00:00Z"
  }'
```

→ `campaignId=11`. Activate → 11 → robot gửi impression vẫn với route 1 (campaign 11 không có route → không match).

Tạo campaign 12 chỉ mua zone mà robot đang ở:
```bash
# Cùng zoneId=1
curl -X POST http://localhost:5000/api/v1/ad-campaigns \
  -H "Content-Type: application/json" \
  -d '{ "packageId": 1, "brandId": 1, "zoneIds": [1],
        "campaignName": "Zone only 12", ... }'
```
→ Activate → robot impression → campaign 12 match **zone**.

**Verify log:**
```sql
SELECT * FROM AD_CAMPAIGN_LOG WHERE AdCampaignID IN (10, 11, 12) AND ActionType='RoutePass';
```

#### Bước 9: Test Shelf trúng

Tạo campaign 13 chỉ có Shelf (SemanticObjectID = ID của shelf chứa tọa độ (x,y)):
```bash
curl -X POST http://localhost:5000/api/v1/ad-campaigns \
  -H "Content-Type: application/json" \
  -d '{ "packageId": 1, "brandId": 1,
        "semanticObjectId": 1,
        "campaignName": "Shelf only 13", ... }'
```
→ Activate → robot gửi impression với tọa độ nằm trong shelf → campaign 13 match **shelf**.

**Verify:**
```sql
SELECT * FROM AD_CAMPAIGN_LOG WHERE AdCampaignID = 13 AND ActionType='RoutePass';
```

#### Bước 10: Test trùng (cùng 1 impression match cả 3 luồng)

Campaign 10 đã mua cả route + zone + shelf. Robot đến kệ chứa tọa độ thuộc zone 1, route hiện tại = 1.

→ Ghi impression → campaign 10 match cả 3 luồng.

**Verify charge:**
```sql
SELECT * FROM AD_CAMPAIGN_LOG WHERE AdCampaignID = 10 AND ActionType='RoutePass' ORDER BY Timestamp DESC;
```
**Mong đợi:** `ChargedAmount` = `MAX(RoutePriceCharged, ZonePriceCharged, ShelfPriceCharged)`, không cộng dồn.

#### Bước 11: Pause + Resume

```bash
# Pause
curl -X POST http://localhost:5000/api/v1/ad-campaigns/10/pause \
  -H "Content-Type: application/json" -d '{"reason":"Test pause"}'

# Verify
SELECT Status FROM AD_CAMPAIGN WHERE AdCampaignID = 10;  -- Paused

# Re-activate (không charge lại)
curl -X POST http://localhost:5000/api/v1/ad-campaigns/10/activate
```

**Mong đợi:** Status → `Active`. `amountCharged = 0` (vì resuming từ Paused).

#### Bước 12: Cancel + Refund

```bash
curl -X POST http://localhost:5000/api/v1/ad-campaigns/10/cancel
```

**Verify:**
```sql
SELECT Status FROM AD_CAMPAIGN WHERE AdCampaignID = 10;  -- Canceled
SELECT Wallet FROM BRAND WHERE BrandID = 1;  -- Tăng lên theo refund
```

#### Bước 13: Validate lỗi

| Test | Request | Mong đợi |
|------|---------|----------|
| Activate không có targeting | Tạo campaign không route/zone/shelf → activate | `400` `CampaignNoTargeting` |
| Wallet không đủ | Brand wallet = 0 → activate | `400` `InsufficientWalletBalance` |
| Update campaign Active | Update campaign đang Active | `400` `CampaignNotEditable` |
| Delete campaign Active | Delete campaign đang Active | `400` `CannotDeleteActiveCampaign` |
| Impression slot không hợp lệ | `slotId = 0`, `xCoord = -1` | `400` validation |

---

## 4. Checklist Chấp Nhận (Acceptance)

| # | Mục | Pass/Fail |
|---|-----|-----------|
| 1 | `PriceRoute`, `PriceZone`, `PriceShelf` có giá trị sau migration | ☐ |
| 2 | Tạo campaign với chỉ `routeIds` → Activate thành công, trừ `PricePackage + PriceRoute × count` | ☐ |
| 3 | Tạo campaign với chỉ `zoneIds` → Activate thành công | ☐ |
| 4 | Tạo campaign với chỉ `semanticObjectId` → Activate thành công, trừ `PriceShelf` | ☐ |
| 5 | Tạo campaign không có targeting → Activate trả 400 | ☐ |
| 6 | Snapshot giá route/zone/shelf đúng theo `PriceRoute/PriceZone/PriceShelf` của package | ☐ |
| 7 | Robot có route Active → impression match Route, ghi log RoutePass | ☐ |
| 8 | Robot ở zone có zone targeting → impression match Zone, ghi log | ☐ |
| 9 | Robot ở kệ có shelf targeting → impression match Shelf, ghi log | ☐ |
| 10 | Cùng 1 campaign match cả 3 luồng → charge MAX, không cộng dồn | ☐ |
| 11 | Pause → Resume → không charge thêm | ☐ |
| 12 | Cancel → refund đúng | ☐ |
| 14 | SmartMart brand có `IsSystemBrand = true` → ưu tiên hiển thị trong deals | ☐ |
| 15 | SmartMart boost `systemBrandBonus` được tính trong recommendations | ☐ |
| 13 | Update semantic object → snapshot shelf re-charge đúng giá mới | ☐ |

---

## 5. Test Nhanh Bằng Script PowerShell

```powershell
# Chạy tất cả trong 1 lần — lưu file test-ads.ps1 và chạy
$base = "http://localhost:5000"
$brandId = 1
$pkgId = 1
$robotCode = "ROBOT01"

# 1. Create campaign với cả 3 luồng
$body = @{
    packageId       = $pkgId
    brandId         = $brandId
    semanticObjectId = 1
    zoneIds         = @(1)
    routeIds        = @(1)
    campaignName    = "E2E PowerShell"
    startDate       = "2026-07-01T00:00:00Z"
    endDate         = "2027-01-01T00:00:00Z"
    productIds      = @(101)
} | ConvertTo-Json -Depth 5

$resp = Invoke-RestMethod -Method Post "$base/api/v1/ad-campaigns/with-products" `
    -ContentType "application/json" -Body $body
Write-Host "Created campaign $($resp.adCampaignId)"

# 2. Activate
$act = Invoke-RestMethod -Method Post "$base/api/v1/ad-campaigns/$($resp.adCampaignId)/activate"
Write-Host "Charged $($act.amountCharged), balance $($act.remainingWalletBalance)"

# 3. Assign robot to route
Invoke-RestMethod -Method Post "$base/api/v1/ad-routes/1/assign/1" | Out-Null

# 4. Robot impression
$impBody = @{ slotId = 5; xCoord = 10; yCoord = 20; memberId = 1 } | ConvertTo-Json
$imp = Invoke-RestMethod -Method Post "$base/api/robots/$robotCode/impression" `
    -ContentType "application/json" -Body $impBody
Write-Host "Impressed $($imp.impressionCount), total $($imp.totalChargedAmount)"
```

---

## 6. Troubleshooting

| Lỗi | Nguyên nhân | Fix |
|------|-------------|-----|
| `Cannot insert duplicate key in object 'dbo.AD_PACKAGE'` | Migration chạy 2 lần | OK — file idempotent. Chỉ check `PRINT` message |
| `PriceZone invalid column` | Migration chưa chạy | Chạy `migration_all_in_one.sql` |
| `CampaignNoTargeting` | Campaign không có targeting nào | Thêm ít nhất 1 route/zone/shelf |
| `InsufficientWalletBalance` | Brand wallet < totalCost | Nạp tiền: `UPDATE BRAND SET Wallet = 999999999 WHERE BrandID = 1` |
| `AdRobotNoActiveRoute` | Robot chưa gán route | Gọi `POST /api/v1/ad-routes/{id}/assign/{robotId}` |
| `AdNoContext` | Tọa độ không thuộc shelf nào + không có zone | Kiểm tra `SEMANTIC_OBJECT.XMin/XMax/YMin/YMax` |
| `CampaignNotFound` | ID sai | Verify `SELECT * FROM AD_CAMPAIGN` |

---

## 7. Tóm Tắt File Liên Quan

| File | Vai trò |
|------|---------|
| `db/migrations/migration_all_in_one.sql` | Migration tổng hợp (idempotent) |
| `src/SmartMarketBot.API/Controllers/AdCampaignsController.cs` | 11 endpoints CRUD campaign |
| `src/SmartMarketBot.API/Controllers/AdCampaignController.cs` | 6 endpoints robot playlist |
| `src/SmartMarketBot.API/Controllers/AdRoutesController.cs` | 7 endpoints route |
| `src/SmartMarketBot.API/Controllers/AdPackagesController.cs` | 5 endpoints package |
| `src/SmartMarketBot.API/Controllers/AdResourcesController.cs` | 6 endpoints resource |
| `src/SmartMarketBot.API/Controllers/RobotImpressionController.cs` | **1 endpoint impression (3-luồng)** |
| `src/SmartMarketBot.Infrastructure/Services/AdCampaignService.cs` | Logic Activate + 3 luồng charge |
| `src/SmartMarketBot.Infrastructure/Services/AdAnalyticsService.cs` | Logic UNION 3 luồng |
 ư