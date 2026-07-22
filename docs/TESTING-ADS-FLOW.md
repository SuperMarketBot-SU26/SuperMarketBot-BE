# Hướng Dẫn Test Luồng Quảng Cáo (Ads Flow)

> File test toàn bộ hệ thống quảng cáo của SmartMarketBot, bao gồm các chức năng vừa implement theo `ADR-ROUTE-INTEGRATION-PLAN.md`.

---

## 1. Tổng Quan Luồng Quảng Cáo

### 1.1 Actors
- **Brand**: nhà quảng cáo (đăng ký, nạp ví, tạo campaign, mua targeting)
- **Robot**: thiết bị IoT, broadcast quảng cáo theo vị trí hoặc playlist
- **Member**: khách hàng cuối, nhận sponsored recommendation + click ad

### 1.2 Sơ Đồ Luồng

```
┌──────────────────────────────────────────────────────────────────┐
│                    BRAND FLOW (Web Admin)                        │
├──────────────────────────────────────────────────────────────────┤
│ 1. Tạo Brand                  (POST /api/v1/brands)               │
│ 2. Nạp ví                     (POST /api/v1/brands/{id}/wallet/   │
│                                topup)                             │
│ 3. Chọn Package               (GET /api/v1/ad-packages)           │
│ 4. Upload tài nguyên quảng cáo (POST /api/v1/ad-resources/upload) │
│ 5. Tạo Campaign + Sponsored   (POST /api/v1/ad-campaigns/        │
│    Products (targeting)        with-products)                     │
│ 6. Activate Campaign           (POST /api/v1/ad-campaigns/{id}/   │
│    (charge wallet 3 luồng)     activate)                          │
│ 7. Pause / Cancel             (POST /pause, /cancel)              │
│ 8. Xem logs                   (GET /api/v1/ad-campaigns/{id}/logs)│
└──────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌──────────────────────────────────────────────────────────────────┐
│                    ROUTE INTEGRATION (Admin)                       │
├──────────────────────────────────────────────────────────────────┤
│ 9. Tạo AdRoute                (POST /api/v1/ad-routes)            │
│    - Mode: Autonomous          (sequential playlist)              │
│    - Mode: ZoneShelf           (AABB detection)                   │
│ 10. Assign Route cho Robot    (POST /api/v1/ad-routes/{id}/       │
│                                assign/{robotId})                  │
└──────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌──────────────────────────────────────────────────────────────────┐
│                    ROBOT BROADCAST (IoT)                           │
├──────────────────────────────────────────────────────────────────┤
│ 11. Pre-compile full route    (GET /api/v1/ad-campaign/robot/     │
│     (1 lần khi start)         {code}/broadcast/route)             │
│ 12. Lấy playlist theo vị trí  (GET /api/v1/ad-campaign/robot/    │
│     (mỗi 5-10s)               {code}/broadcast/now?x=&y=)         │
│ 13. Log impression            (POST /api/robots/impressions)      │
└──────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌──────────────────────────────────────────────────────────────────┐
│                    MEMBER FLOW (Mobile)                            │
├──────────────────────────────────────────────────────────────────┤
│ 14. Xem gợi ý quảng cáo       (GET /api/members/{id}/sponsored-  │
│     (khi scan shelf)           recommendations)                   │
│ 15. Click quảng cáo           (POST /api/v1/ad-campaign/log-     │
│     (ghi log + anti-fraud)     interaction)                       │
│ 16. Bind session              (PUT /api/v1/ad-campaign/session/   │
│                                bind)                              │
└──────────────────────────────────────────────────────────────────┘
```

### 1.3 Bảng DB Liên Quan

| Bảng | Vai trò |
|------|---------|
| `BRAND` | Brand + Wallet Balance |
| `AD_PACKAGE` | Các gói giá (PricePackage, PriceRoute, PriceZone, PriceShelf) |
| `AD_RESOURCE` | Media (image/video/text) |
| `AD_CAMPAIGN` | Campaign + Status + Dates |
| `SPONSORED_PRODUCT` | Mapping Campaign ↔ Product + Priority |
| `AD_CAMPAIGN_ZONE` | Targeting theo Zone |
| `AD_CAMPAIGN_ROUTE` | Targeting theo RobotRoute |
| `AD_CAMPAIGN_LOG` | Log impression/click + FraudDetected |
| `AD_ROUTE` | **MỚI** — Ad route có 2 mode (Autonomous/ZoneShelf) |
| `AD_ROUTE_NODE` | **MỚI** — Có `ZoneID` để group playlist |
| `ROBOT_AD_ROUTE_ASSIGNMENT` | **MỚI** — Gán AdRoute cho Robot |
| `SEMANTIC_OBJECT` | Kệ/Zone có AABB (XMin,YMin,XMax,YMax) |

---

## 2. Chuẩn Bị Môi Trường

### 2.1 Yêu cầu
- .NET 10 SDK
- SQL Server đã chạy migration `migration_ad_route_mode.sql`
- Swagger tại `http://localhost:5000/swagger`

### 2.2 Chạy Migration

```sql
-- File: db/migrations/migration_ad_route_mode.sql
-- Đã viết idempotent: tự tạo bảng nếu chưa có
sqlcmd -S <server> -d SuperMarketBot -U <user> -P <pwd> \
       -i db/migrations/migration_ad_route_mode.sql
```

### 2.3 Verify Migration

```sql
-- Expected: tất cả = 1
SELECT
  (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME='AD_ROUTE' AND COLUMN_NAME='IsAutonomous') AS HasIsAutonomous,
  (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME='AD_ROUTE' AND COLUMN_NAME='SemanticObjectID') AS HasSemanticObjectID,
  (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME='AD_ROUTE_NODE' AND COLUMN_NAME='ZoneID') AS HasZoneID,
  (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_NAME='ROBOT_AD_ROUTE_ASSIGNMENT') AS HasAssignmentTable;
```

### 2.4 Khởi Động App

```bash
dotnet run --project src/SmartMarketBot.API/SmartMarketBot.API.csproj
# Mở: http://localhost:5000/swagger
```

### 2.5 Auth

Một số endpoint yêu cầu JWT. Lấy token qua:

```http
POST /api/auth/login
Content-Type: application/json

{ "username": "brand001", "password": "P@ssw0rd" }

→ Response: { "accessToken": "eyJhbGc...", "refreshToken": "..." }
```

Gắn vào header các request sau:
```http
Authorization: Bearer eyJhbGc...
```

---

## 3. Module 1: BRAND & WALLET

### 3.1 List Brand

```http
GET /api/v1/brands
```
→ Trả danh sách brand. Verify DB có row trong `BRAND`.

### 3.2 Tạo Brand

```http
POST /api/v1/brands
{
  "brandName": "Vinamilk",
  "isSystemBrand": false
}
```
**Verify:** DB `BRAND` có row mới với `BrandID` tự tăng.

### 3.3 Nạp Ví (TopUp)

```http
POST /api/v1/brands/{brandId}/wallet/topup
{
  "amount": 1000000,
  "description": "Nạp ví tháng 7"
}
```

**Verify:**
- Response có `newBalance`
- DB `BRAND.WalletBalance` tăng đúng `amount`
- Nếu có bảng transaction log → check có row mới

### 3.4 Admin Deposit (System brand nạp cho user brand)

```http
POST /api/v1/brands/{brandId}/deposit
{
  "amount": 500000,
  "note": "Khuyến mãi"
}
```

### 3.5 Get Wallet

```http
GET /api/v1/brands/{brandId}
```
→ Response `walletBalance` phải khớp DB.

---

## 4. Module 2: AD PACKAGE & RESOURCE

### 4.1 List Package

```http
GET /api/v1/ad-packages
```

**Verify response có các field:**
- `pricePackage`, `priceRoute`, `priceZone`, `priceShelf`
- `adScore` (dùng để sắp xếp priority khi broadcast)

### 4.2 Upload Resource (Image/Video)

```http
POST /api/v1/ad-resources/upload
Content-Type: multipart/form-data

Form fields:
- AdCampaignId: 1
- ResourceType: "Image"  (Image | Video | Text)
- File: <binary>
- ContentText: "Sữa tươi Vinamilk 100%"
- Resolution: "1920x1080"
```

**Verify:**
- Response có `resourceUrl` (link Cloudinary/local)
- DB `AD_RESOURCE` có row mới
- File được lưu trong folder `ad-resources/`

### 4.3 Tạo Resource bằng URL

```http
POST /api/v1/ad-resources
{
  "adCampaignId": 1,
  "resourceType": "Video",
  "resourceUrl": "https://example.com/ad.mp4",
  "contentText": "Video giới thiệu",
  "resolution": "1920x1080"
}
```

### 4.4 List Resource theo Campaign

```http
GET /api/v1/ad-resources/campaign/{campaignId}
```

### 4.5 Update Status Resource

```http
PATCH /api/v1/ad-resources/{resourceId}/status
{ "status": "Active" }   // Active | Inactive
```

### 4.6 Delete Resource

```http
DELETE /api/v1/ad-resources/{resourceId}
```

---

## 5. Module 3: AD CAMPAIGN (Phần Lõi)

### 5.1 Tạo Campaign với Targeting 3 Luồng

```http
POST /api/v1/ad-campaigns/with-products
{
  "packageId": 1,
  "brandId": 1,
  "campaignName": "Vinamilk - Sữa tươi tháng 7",
  "budget": 5000000,
  "startDate": "2026-07-22T00:00:00Z",
  "endDate": "2026-08-22T00:00:00Z",
  "semanticObjectId": 5,                  // Targeting KỆ
  "zoneIds": [1, 2],                      // Targeting ZONE
  "routeIds": [3],                        // Targeting ROUTE
  "productIds": [101, 102, 103],          // SponsoredProducts
  "priority": 5
}
```

**Verify response:** `adCampaignId` được tạo.

### 5.2 Verify DB sau khi tạo

```sql
-- Campaign
SELECT * FROM AD_CAMPAIGN WHERE AdCampaignID = <id>;
-- SponsoredProducts
SELECT * FROM SPONSORED_PRODUCT WHERE AdCampaignID = <id>;
-- Targeting Zone
SELECT * FROM AD_CAMPAIGN_ZONE WHERE AdCampaignID = <id>;
-- Targeting Route
SELECT * FROM AD_CAMPAIGN_ROUTE WHERE AdCampaignID = <id>;
```

### 5.3 List Campaign

```http
GET /api/v1/ad-campaigns?pageNumber=1&pageSize=10&brandId=1
```

### 5.4 Get Campaign Detail

```http
GET /api/v1/ad-campaigns/{campaignId}
```

### 5.5 Update Campaign

```http
PUT /api/v1/ad-campaigns/{campaignId}
{
  "campaignName": "Vinamilk - Updated",
  "budget": 6000000,
  ...
}
```

### 5.6 ACTIVATE — Charge Wallet (Test Quan Trọng)

```http
POST /api/v1/ad-campaigns/{campaignId}/activate
```

**Test case quan trọng (theo rule 30-backend-guidelines.mdc):**

| Test | Input | Expected |
|------|-------|----------|
| Đủ tiền + 0 route + 0 zone + 0 shelf | Brand có 5M | Charge = `PricePackage`, Status = Active |
| Đủ tiền + 2 route + 1 zone + 1 shelf | Brand có 5M | Charge = `PricePackage + PriceRoute*2 + PriceZone*1 + PriceShelf` |
| **Không đủ tiền** | Brand có 100k | Throw `InvalidWalletBalance`, Status giữ Inactive, transaction rollback |
| **System Brand** | `isSystemBrand = true` | Charge = 0 |
| SystemBrand + 5 route | | Charge = 0 (vẫn được activate) |

**Verify sau Activate:**
```sql
SELECT WalletBalance FROM BRAND WHERE BrandID = 1;
-- Phải giảm đúng số tiền

SELECT Status, StartDate, EndDate FROM AD_CAMPAIGN WHERE AdCampaignID = <id>;
-- Status = 'Active'
```

### 5.7 Pause Campaign

```http
POST /api/v1/ad-campaigns/{campaignId}/pause
{ "reason": "Hết hàng tạm thời" }
```

**Verify:**
- Status = `Paused`
- Không hiển thị trong playlist broadcast nữa

### 5.8 Cancel Campaign

```http
POST /api/v1/ad-campaigns/{campaignId}/cancel
```

**Verify:**
- Status = `Cancelled`
- EndDate = now

### 5.9 Assign Routes (sau khi tạo)

```http
POST /api/v1/ad-campaigns/{campaignId}/routes
{ "routeIds": [4, 5] }
```

**Verify:** Charge thêm `PriceRoute * count` vào wallet (nếu campaign đang Paused).

### 5.10 Get Assigned Routes

```http
GET /api/v1/ad-campaigns/{campaignId}/routes
```

### 5.11 Xem Logs

```http
GET /api/v1/ad-campaigns/{campaignId}/logs?pageNumber=1&pageSize=20
```

### 5.12 Targeting Context (cho UI single-fetch)

```http
GET /api/v1/ad-campaign/{campaignId}/targeting-context?floorId=1
```

→ Trả: `mapId` + `shelves[]` + `routes[]` + `assignedRouteIds[]`.

**Verify:** FE có thể render TargetingSelector chỉ với 1 call.

### 5.13 Delete Campaign

```http
DELETE /api/v1/ad-campaigns/{campaignId}
```

---

## 6. Module 4: SPONSORED PRODUCT

### 6.1 List Sponsored theo Campaign

```http
GET /api/v1/sponsored-products/campaign/{campaignId}
```

### 6.2 Thêm 1 Sponsored

```http
POST /api/v1/sponsored-products
{
  "adCampaignId": 1,
  "productId": 104,
  "priority": 10
}
```

### 6.3 Bulk Sponsored

```http
POST /api/v1/sponsored-products/bulk
{
  "adCampaignId": 1,
  "items": [
    { "productId": 104, "priority": 5 },
    { "productId": 105, "priority": 8 }
  ]
}
```

### 6.4 Update Priority

```http
PUT /api/v1/sponsored-products/{sponsoredId}/priority
{ "priority": 15 }
```

### 6.5 Update Status

```http
PATCH /api/v1/sponsored-products/{sponsoredId}/status
{ "status": "Active" }   // Active | Paused | Removed
```

### 6.6 Delete Sponsored

```http
DELETE /api/v1/sponsored-products/{sponsoredId}
```

---

## 7. Module 5: AD ROUTE — Module Vừa Implement

### 7.1 Tạo AdRoute với Mode Autonomous

```http
POST /api/v1/ad-routes
{
  "routeName": "Tour quảng cáo Zone Rau củ",
  "description": "Robot chạy tự động qua các node",
  "isAutonomous": true,
  "semanticObjectId": null,
  "nodes": [
    { "nodeId": 10, "sequenceOrder": 1, "dwellTimeSeconds": 30, "zoneId": 1 },
    { "nodeId": 15, "sequenceOrder": 2, "dwellTimeSeconds": 45, "zoneId": 1 },
    { "nodeId": 25, "sequenceOrder": 3, "dwellTimeSeconds": 30, "zoneId": 2 },
    { "nodeId": 30, "sequenceOrder": 4, "dwellTimeSeconds": 30, "zoneId": 3 }
  ],
  "campaignIds": [1, 2]
}
```

**Verify:**
- Response có `adRouteId`, `isAutonomous=true`, `nodes[4]`
- DB `AD_ROUTE` có row, `AD_ROUTE_NODE` có 4 rows (mỗi row có `ZoneID`)

### 7.2 Tạo AdRoute với Mode ZoneShelf

```http
POST /api/v1/ad-routes
{
  "routeName": "Track theo kệ sữa",
  "description": "Khi robot đến gần kệ sữa thì phát QC",
  "isAutonomous": false,
  "semanticObjectId": 5,
  "nodes": [],
  "campaignIds": [3]
}
```

**Verify:**
- `isAutonomous=false`, `semanticObjectId=5`
- DB `AD_ROUTE` có row với `SemanticObjectID=5`

### 7.3 Update AdRoute

```http
PUT /api/v1/ad-routes/{routeId}
{
  "routeName": "Updated route",
  "description": "...",
  "isActive": true,
  "isAutonomous": true,
  "semanticObjectId": null,
  "nodes": [...],
  "campaignIds": [...]
}
```

**Verify:** Update cả mode + nodes + campaigns.

### 7.4 List AdRoutes

```http
GET /api/v1/ad-routes?pageNumber=1&pageSize=10&isActive=true
```

### 7.5 Get AdRoute by ID

```http
GET /api/v1/ad-routes/{routeId}
```

### 7.6 Get Active Route cho Robot

```http
GET /api/v1/ad-routes/robot/{robotId}/active
```
→ Trả AdRoute hiện đang gán cho robot (từ bảng `ROBOT_AD_ROUTE_ASSIGNMENT`).

### 7.7 Assign AdRoute cho Robot

```http
POST /api/v1/ad-routes/{routeId}/assign/{robotId}
```

**Verify (Test quan trọng — theo ADR):**
```sql
-- Row mới PHẢI nằm trong ROBOT_AD_ROUTE_ASSIGNMENT
SELECT * FROM ROBOT_AD_ROUTE_ASSIGNMENT WHERE RobotID = <robotId> ORDER BY AssignedAt DESC;
-- Row cũ trong ROUTE_ASSIGNMENT KHÔNG được tạo
SELECT * FROM ROUTE_ASSIGNMENT WHERE RobotID = <robotId>;
-- (phải trả 0 row nếu robot này chưa từng được assign RobotRoute)
```

### 7.8 Delete AdRoute

```http
DELETE /api/v1/ad-routes/{routeId}
```

**Verify:** Cascade xóa `AD_ROUTE_NODE`, `ROBOT_AD_ROUTE_ASSIGNMENT` liên quan.

---

## 8. Module 6: BROADCAST — Phần Vừa Implement

### 8.1 Pre-compile Full Route (Autonomous)

```http
GET /api/v1/ad-campaign/robot/{robotCode}/broadcast/route
```

**Response mẫu (AdRouteBroadcastDto):**
```json
{
  "robotId": 1,
  "adRouteId": 5,
  "routeName": "Tour quảng cáo Zone Rau củ",
  "isAutonomous": true,
  "stops": [
    {
      "sequenceOrder": 1,
      "nodeId": 10,
      "nodeName": "Node Rau củ A",
      "dwellTimeSeconds": 30,
      "zoneId": 1,
      "zoneName": "Rau củ",
      "playlist": [
        {
          "sponsoredId": 10,
          "adCampaignId": 1,
          "campaignName": "Vinamilk - Sữa tươi",
          "productName": "Sữa Vinamilk 1L",
          "productPrice": 28000,
          "priority": 10,
          "adScore": 85,
          "mediaContents": [
            { "resourceType": "Image", "resourceUrl": "...", "contentText": "..." }
          ]
        }
      ]
    },
    { "sequenceOrder": 2, ... },
    ...
  ],
  "generatedAt": "2026-07-22T10:30:00Z"
}
```

**Test:**
1. Tạo AdRoute với 3 node, mỗi node thuộc 1 zone khác nhau
2. Assign cho robot có `robotCode = "RB01"`
3. Gọi `/broadcast/route` → response phải có 3 stops
4. Mỗi stop có playlist của campaign active trong zone tương ứng
5. Verify playlist được sort theo `AdScore DESC, Priority DESC`

### 8.2 Get Playlist Theo Vị Trí Hiện Tại

```http
GET /api/v1/ad-campaign/robot/{robotCode}/broadcast/now?x=12&y=8
```

**Response (AdPlaylistDto):**
```json
{
  "robotId": 1,
  "mode": "Autonomous",   // Autonomous | ZoneShelf | None
  "nodeId": 10,
  "zoneId": 1,
  "zoneName": "Rau củ",
  "resources": [ ... ],
  "generatedAt": "..."
}
```

#### 8.2.1 Test Mode Autonomous

**Setup:** AdRoute IsAutonomous=true, đã assign cho robot RB01.

**Test:**
1. Lấy `XCoord, YCoord` của một node trong route (vd node 10 có x=12, y=8)
2. Gọi `/broadcast/now?x=12&y=8`
3. **Expected:**
   - `mode = "Autonomous"`
   - `nodeId = 10`
   - `zoneId = <zone của node 10>`
   - `resources` chứa campaign active trong zone đó

**Test ngoài node:**
- Gọi với `x=999, y=999` (không có node nào)
- **Expected:** `mode = "Autonomous"`, `resources = []`

#### 8.2.2 Test Mode ZoneShelf (AABB Detection)

**Setup:** AdRoute IsAutonomous=false với `SemanticObjectID = 5` đã assign cho robot.

**Pre-check:**
```sql
SELECT ObjectID, Label, XMin, XMax, YMin, YMax
FROM SEMANTIC_OBJECT
WHERE ObjectID = 5;
-- VD: XMin=10, XMax=20, YMin=5, YMax=15
```

**Test:**
1. Gọi `/broadcast/now?x=15&y=10` (nằm trong AABB)
   - **Expected:** `mode = "ZoneShelf"`, `resources` chứa campaign active targeting kệ 5

2. Gọi `/broadcast/now?x=5&y=5` (NGOÀI AABB)
   - **Expected:** `mode = "ZoneShelf"`, `resources = []`

3. Gọi `/broadcast/now?x=10&y=5` (biên)
   - **Expected:** có thể nằm trong hoặc ngoài (boundary inclusive). Check theo code `XMin <= x <= XMax`.

#### 8.2.3 Test Mode None

**Setup:** Robot chưa được assign AdRoute nào.

**Test:**
1. Gọi `/broadcast/now?x=12&y=8`
2. **Expected:** `mode = "None"`, `resources = []`

### 8.3 Get Playlist Cho Node Cụ Thể

```http
GET /api/v1/ad-campaign/robot-playlist/{robotId}/node/{nodeId}
```

**Verify:** Trả playlist các campaign active trong zone của node.

### 8.4 Get Playlist Cho Zone

```http
GET /api/v1/ad-campaign/robot-playlist/{robotId}/zone/{zoneId}
```

### 8.5 Get Playlist Cho SemanticObject

```http
GET /api/v1/ad-campaign/robot-playlist/{robotId}?semanticObjectId=5
```

### 8.6 Get Autonomous Route (Legacy endpoint)

```http
GET /api/v1/ad-campaign/robot-playlist/{robotId}/autonomous
```

> **Lưu ý:** Endpoint này vẫn dùng `ROBOT_AD_ROUTE_ASSIGNMENT` (sau khi sửa).

---

## 9. Module 7: LOG & ANTI-FRAUD

### 9.1 Log Interaction

```http
POST /api/v1/ad-campaign/log-interaction
{
  "adCampaignId": 1,
  "actionType": "Click",     // Click | Impression | View | Purchase
  "robotId": 1,
  "memberId": 5,
  "sessionId": "session-uuid-001",
  "sponsoredId": 10,
  "productId": 101,
  "semanticObjectId": 5,
  "zoneId": 1,
  "xCoord": 12.5,
  "yCoord": 7.2
}
```

### 9.2 Bind Session

```http
PUT /api/v1/ad-campaign/session/bind
{
  "sessionId": "session-uuid-001",
  "robotId": 1,
  "memberId": 5,
  "semanticObjectId": 5
}
```

### 9.3 Anti-Fraud Test (Quan Trọng Theo Rule 30)

**Test case 1: Spam Click**
```bash
# Click 4 lần liên tiếp trong < 30s cùng memberId + sessionId
for i in 1 2 3 4; do
  curl -X POST http://localhost:5000/api/v1/ad-campaign/log-interaction \
    -H "Content-Type: application/json" \
    -d '{"adCampaignId":1,"actionType":"Click","memberId":5,"sessionId":"test-001"}'
done
```
- Lần 1-3: `isFraud = false`
- Lần 4: `isFraud = true`, log `ActionType = "FraudDetected"`

**Verify:**
```sql
SELECT * FROM AD_CAMPAIGN_LOG
WHERE MemberID = 5 AND SessionID = 'test-001'
ORDER BY CreatedAt DESC;
```

### 9.4 Log Impression (Robot ghi)

```http
POST /api/robots/impressions
{
  "robotId": 1,
  "adCampaignId": 1,
  "sponsoredId": 10,
  "xCoord": 12.5,
  "yCoord": 7.2,
  "durationMs": 5000
}
```

---

## 10. Module 8: SPONSORED RECOMMENDATION (Member-side)

### 10.1 Get Recommendations cho Member

```http
GET /api/members/{memberId}/sponsored-recommendations?slotId=10
```

**Verify (theo rule 30 — Stale Time Avoidance):**
- Kết quả được tính tại thời điểm request (không dùng static DateTime)
- Weekend bonus áp dụng nếu current day là Sat/Sun
- Lọc allergy của member
- Sort theo `AdScore × weekendBonus × brandBoost` desc

### 10.2 Test Allergy Filter

**Setup:** Member có allergy với "Hải sản".

```http
GET /api/members/{memberId}/sponsored-recommendations
```

**Verify:**
- KHÔNG chứa sản phẩm có health-tag "Hải sản"
- Nếu lỡ chứa → check `Localizer.Get("Error_AllergyAlert")` được gọi (xem log/middleware)

---

## 11. Luồng End-to-End Hoàn Chỉnh

### Scenario 1: Brand mua quảng cáo + Robot phát

```text
Bước 1.  Login brand (qua Auth) → JWT
Bước 2.  Tạo brand           → POST /api/v1/brands
Bước 3.  Nạp ví 1 triệu      → POST /api/v1/brands/{id}/wallet/topup
Bước 4.  Xem packages        → GET /api/v1/ad-packages (chọn package 1)
Bước 5.  Upload 2 image QC   → POST /api/v1/ad-resources/upload
Bước 6.  Tạo campaign        → POST /api/v1/ad-campaigns/with-products
           - targeting: zone 1, kệ 5, route 3
           - 3 sản phẩm, priority 5-10
Bước 7.  Activate campaign   → POST /api/v1/ad-campaigns/{id}/activate
           - Charge = PricePkg + PriceRoute + PriceZone + PriceShelf
           - Status = Active
Bước 8.  Tạo AdRoute         → POST /api/v1/ad-routes
           - isAutonomous = true, 3 node có zoneId
Bước 9.  Assign robot        → POST /api/v1/ad-routes/{id}/assign/{robotId}
           - DB: ROBOT_AD_ROUTE_ASSIGNMENT có row mới
Bước 10. Robot khởi động     → GET /api/v1/ad-campaign/robot/{code}/broadcast/route
           - Response: 3 stops, mỗi stop có playlist
Bước 11. Robot đến node 10  → GET /api/v1/ad-campaign/robot/{code}/broadcast/now?x=&y=
           - Mode = Autonomous, playlist của zone 1
Bước 12. Robot đến kệ 5     → GET /broadcast/now?x=15&y=10
           - Mode = ZoneShelf, playlist của kệ 5
Bước 13. Robot ghi impression → POST /api/robots/impressions
Bước 14. Member click QC     → POST /api/v1/ad-campaign/log-interaction
Bước 15. Brand xem logs      → GET /api/v1/ad-campaigns/{id}/logs
```

### Scenario 2: Pause + Reactivate

```text
Bước 1.  Campaign đang Active
Bước 2.  Pause               → POST /api/v1/ad-campaigns/{id}/pause
           - Status = Paused
Bước 3.  Test broadcast      → /broadcast/now trả playlist []
Bước 4.  Activate lại        → POST /api/v1/ad-campaigns/{id}/activate
           - Charge lại nếu hết hạn
           - Status = Active
```

### Scenario 3: 2 cùng Active cho 1 robot

```text
Bước 1.  AdRoute A assigned cho robot 1 (status=Active)
Bước 2.  AdRoute B assigned cho robot 1
Bước 3.  Verify:           SELECT * FROM ROBOT_AD_ROUTE_ASSIGNMENT WHERE RobotID = 1;
           - AdRoute A → Status = 'Completed'
           - AdRoute B → Status = 'Active'
Bước 4.  /broadcast/route    → trả playlist của AdRoute B
```

---

## 12. Test Negative / Edge Cases

### 12.1 Wallet không đủ

```text
1. Brand có wallet = 100k
2. Tạo campaign targeting 5 route × 200k = 1M
3. Activate → Expected: HTTP 400 với message "InvalidWalletBalance"
4. Verify: Status giữ Inactive, wallet không bị trừ
```

### 12.2 Campaign EndDate trong quá khứ

```text
1. Tạo campaign EndDate = 2020-01-01
2. Activate → OK nhưng playlist sẽ rỗng (đã hết hạn)
3. /broadcast/now → resources = []
```

### 12.3 Node không tồn tại

```text
1. Tạo AdRoute với nodeId = 99999 (không tồn tại trong NAVIGATION_NODE)
2. Expected: HTTP 400/500, hoặc EF Core FK violation
```

### 12.4 Assign AdRoute Inactive

```text
1. Tạo AdRoute IsActive = false
2. Assign → Expected: HTTP 400 với message "CannotAssignInactiveRoute"
```

### 12.5 Robot không tồn tại

```text
1. POST /api/v1/ad-routes/1/assign/99999
2. Expected: HTTP 404 với message "RobotNotFound"
```

### 12.6 Concurrent Assign

```text
1. Mở 2 tab, cùng assign AdRoute 1 cho robot 1
2. Expected: cả 2 thành công, chỉ row cuối cùng status=Active
```

### 12.7 SemanticObject AABB Edge

```text
1. Shelf AABB: XMin=10, XMax=20, YMin=5, YMax=15
2. Test các điểm biên:
   - x=10, y=5   → trong (inclusive)
   - x=20, y=15  → trong
   - x=9, y=5    → ngoài
   - x=21, y=10  → ngoài
```

### 12.8 Xóa AdRoute có assignment active

```text
1. AdRoute đang assigned cho robot
2. DELETE → Expected: cascade xóa assignment
3. Verify: SELECT FROM ROBOT_AD_ROUTE_ASSIGNMENT WHERE AdRouteID = <id> → 0 rows
4. Verify: /broadcast/now của robot đó giờ trả mode = "None"
```

### 12.9 Log spam cùng memberId nhưng khác sessionId

```text
1. Member 5 spam 4 click/sessionA → fraud=true ở click 4
2. Member 5 click 1 lần/sessionB → fraud=false (session mới)
```

### 12.10 Pause + xem broadcast

```text
1. Campaign A active, gán AdRoute có campaign A
2. Pause A
3. Robot /broadcast/now → playlist KHÔNG chứa campaign A
```

---

## 13. Test Data Nên Có Sẵn

Để test đầy đủ, cần seed trước:

```sql
-- 1 Brand (đã nạp ví)
INSERT INTO BRAND (BrandName, WalletBalance, IsSystemBrand, ...)
VALUES ('Test Brand', 1000000, 0, ...);

-- 1 System Brand (charge = 0)
INSERT INTO BRAND (BrandName, WalletBalance, IsSystemBrand, ...)
VALUES ('System Brand', 0, 1, ...);

-- 3 AdPackage khác giá
INSERT INTO AD_PACKAGE (PackageName, PricePackage, PriceRoute, PriceZone, PriceShelf, AdScore, ...);

-- 3 Zone
INSERT INTO ZONE (ZoneName, MapID, ...);

-- 5 SemanticObject (kệ)
INSERT INTO SEMANTIC_OBJECT (ObjectLabel, ObjectType, XMin, XMax, YMin, YMax, ...);

-- 10 NavigationNode + Edge
INSERT INTO NAVIGATION_NODE (NodeName, XCoord, YCoord, ...);
INSERT INTO NAVIGATION_EDGE (...);

-- 1 Robot
INSERT INTO ROBOT (RobotCode, RobotName, ...);

-- 3 Product
INSERT INTO PRODUCT (ProductName, ProductTypeID, UnitPrice, ...);
```

---

## 14. Checklist Test Tổng Hợp

### Brand & Wallet
- [ ] Tạo brand thành công
- [ ] TopUp wallet tăng đúng số dư
- [ ] Admin deposit hoạt động

### Package & Resource
- [ ] List packages
- [ ] Upload file image (Cloudinary/local)
- [ ] Upload file video
- [ ] List resource theo campaign

### Campaign
- [ ] Tạo campaign với 3 luồng targeting
- [ ] Update campaign
- [ ] **Activate charge wallet đúng** (với SystemBrand = 0)
- [ ] **Activate thất bại khi wallet không đủ** (rollback)
- [ ] Pause → broadcast rỗng
- [ ] Cancel
- [ ] Logs query được

### Sponsored Product
- [ ] CRUD sponsored
- [ ] Bulk create
- [ ] Update priority

### AdRoute (MỚI)
- [ ] Tạo AdRoute IsAutonomous=true với ZoneId trên nodes
- [ ] Tạo AdRoute IsAutonomous=false với SemanticObjectId
- [ ] Update AdRoute cả mode lẫn nodes
- [ ] Assign robot → **DB: ROBOT_AD_ROUTE_ASSIGNMENT có row**
- [ ] Re-assign → assignment cũ chuyển Completed
- [ ] Get active route cho robot
- [ ] Delete AdRoute cascade xóa nodes + assignments

### Broadcast (MỚI)
- [ ] /broadcast/route trả đủ stops + playlist pre-compiled
- [ ] /broadcast/now Mode Autonomous đúng
- [ ] /broadcast/now Mode ZoneShelf AABB đúng
- [ ] /broadcast/now ngoài AABB → rỗng
- [ ] /broadcast/now không có assignment → mode = None

### Log & Anti-Fraud
- [ ] Log impression
- [ ] Log click
- [ ] **4 click trong 30s → click thứ 4 fraud=true**

### Recommendation
- [ ] Member có allergy → recommendation lọc đúng
- [ ] Recommendation có weekend bonus khi T7/CN

---

## 15. Troubleshooting Nhanh

| Lỗi | Nguyên nhân | Cách sửa |
|------|------------|----------|
| `Cannot find AD_ROUTE` | DB chưa có bảng | Chạy `migration_ad_route_mode.sql` |
| `InvalidWalletBalance` | Brand chưa đủ tiền | TopUp trước |
| Playlist rỗng | Campaign chưa Active hoặc EndDate < now | Check `AD_CAMPAIGN.Status`, dates |
| `isFraud=true` liên tục | Spam quá nhanh | Đợi 30s hoặc dùng sessionId khác |
| Mode None | Chưa assign AdRoute cho robot | `POST /api/v1/ad-routes/{id}/assign/{robotId}` |
| Mode ZoneShelf playlist rỗng | Sai AABB hoặc chưa có campaign targeting kệ | Check SEMANTIC_OBJECT + AD_CAMPAIGN_ZONE |
| Build error `ObjectLabel` | EF mapping sai (đã fix) | dotnet build clean |
| 404 robot not found | Sai robotCode (case-sensitive) | Check DB `ROBOT.RobotCode` |

---

## 16. File Liên Quan

- **ADR:** `docs/ADR-ROUTE-INTEGRATION-PLAN.md`
- **Migration:** `db/migrations/migration_ad_route_mode.sql`
- **Rules:** `.cursor/rules/30-backend-guidelines.mdc`
- **Test guide tổng:** `docs/TESTING-GUIDE.md`
- **DDL gốc:** `db/ddl_erd.sql`