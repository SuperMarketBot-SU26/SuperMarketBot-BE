# 🚀 CURSOR PROMPT: HƯỚNG DẪN BUILD BACKEND ASP.NET CORE (.NET 10) CHUẨN CLEAN ARCHITECTURE

> **Cách dùng**: Nhấp vào nút **Copy** toàn bộ nội dung file này và dán vào cửa sổ **Cursor Chat** hoặc **Composer (Ctrl+I)** trong Cursor để yêu cầu AI tự động sinh mã nguồn nền tảng cho dự án.
>
> **Lưu ý**: Hãy chắc chắn bạn đã tạo file database `database.sql` trong thư mục [db/database.sql](file:///e:/FPT%20UNIVERSITY/CN9/Sep401/SuperMarketBot-BE/db/database.sql) để Cursor có thể đọc cấu trúc bảng làm căn cứ sinh code chính xác!

---

```markdown
Bạn là một Kiến trúc sư Phần mềm (Software Architect) cấp cao chuyên nghiệp về .NET. Tôi muốn bạn xây dựng toàn bộ mã nguồn nền tảng (Foundation) cho Backend của dự án **SmartMarketBot** dựa trên cấu trúc cơ sở dữ liệu đã có sẵn trong file `db/database.sql` của dự án này.

### YÊU CẦU CÔNG NGHỆ:
1. **Framework**: ASP.NET Core (.NET 10)
2. **Kiến trúc**: Clean Architecture (4 layers chuẩn)
3. **ORM**: Entity Framework Core 10 (SQL Server) dùng **Fluent API** để cấu hình thực thể
4. **API**: REST API kết hợp **Swagger / OpenAPI** để lập tài liệu
5. **Real-time**: **SignalR** để thông báo vị trí robot và cảnh báo hết hàng cho Web Admin / Mobile App
6. **MQTT**: Thư viện **MQTTnet** để subscribe nhận dữ liệu telemetry từ robot và publish lệnh điều hướng xuống robot

---

### BƯỚC 1: KHỞI TẠO CẤU TRÚC SOLUTION CLEAN ARCHITECTURE
Hãy tạo cấu trúc Solution `SmartMarketBot.sln` bao gồm 4 dự án thành phần dưới đây (sử dụng .NET 10):

1. **SmartMarketBot.Domain** (Class Library):
   - Chứa các thực thể (Entities), Enums, interfaces cơ bản cho Repositories.
   - Không phụ thuộc vào bất kỳ thư viện bên ngoài nào ngoại trừ các kiểu dữ liệu cơ bản.
2. **SmartMarketBot.Application** (Class Library):
   - Chứa các DTOs (Data Transfer Objects), Interfaces cho Services, dịch vụ Logic (như thuật toán tìm đường Dijkstra), ánh xạ AutoMapper/Mapster và các Use Cases.
   - Phụ thuộc vào `Domain`.
3. **SmartMarketBot.Infrastructure** (Class Library):
   - Thực thi kết nối Cơ sở dữ liệu (EF Core `AppDbContext`), Repository cụ thể, cấu hình Client MQTT (`MqttClientService`), kết nối gọi dịch vụ AI ngoài (FastAPI AI Proxy).
   - Phụ thuộc vào `Application`.
4. **SmartMarketBot.API** (Web API):
   - Các Controllers nhận yêu cầu REST, xử lý Middleware, tích hợp xác thực JWT, cấu hình Dependency Injection (DI) trong `Program.cs` và khởi tạo SignalR Hubs (`RobotHub`).
   - Phụ thuộc vào `Infrastructure`.

---

### BƯỚC 2: ĐỊNH NGHĨA CÁC ĐỐI TƯỢNG DOMAIN ENTITIES (Dựa trên database.sql)
Dịch chuyển các bảng cơ sở dữ liệu từ file `db/database.sql` thành các lớp C# Entity trong dự án `SmartMarketBot.Domain/Entities/`. 

#### ⚠️ QUAN TRỌNG VỀ ĐỒNG BỘ AI & n8n:
- Tên lớp C# và cấu hình Mapping bảng tương ứng phải tuân thủ chính xác các bảng đặc thù sau để tránh làm lỗi các script Python và luồng n8n hiện tại:
  1. Lớp `Member.cs` phải map với bảng tên **`Members`** (số nhiều) có các trường: `MemberID`, `FullName`, `PhoneNumber`, `FacePath`, `FaceVector`. Cột `MemberName` là cột computed trong database (không ghi từ EF Core).
  2. Lớp `RobotLog.cs` phải map với bảng tên **`Robot_Logs`** (có dấu gạch dưới) có các trường: `LogID`, `RobotID`, `battery`, `location`, `status`, `timestamp`.
  3. Lớp `Aisle.cs` phải map với bảng **`Aisles`** có cột `IsBlocked` và `BlockReason` tích hợp sẵn.
  4. Lớp `Product.cs` phải map với bảng **`Products`** và có quan hệ tự liên kết `SubstituteProductID` trỏ đến chính thực thể `Product` làm sản phẩm thay thế.

#### Định nghĩa Cấu trúc Mẫu cho một số Thực thể cốt lõi:

- `User.cs`:
```csharp
namespace SmartMarketBot.Domain.Entities;

public class User
{
    public int UserID { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
```

- `Member.cs` (Map với bảng `Members`):
```csharp
namespace SmartMarketBot.Domain.Entities;

public class Member
{
    public int MemberID { get; set; }
    public int? UserID { get; set; }
    public virtual User? User { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? FacePath { get; set; }
    public string? FaceVector { get; set; } // Lưu chuỗi JSON vector nhúng
    
    // MemberName là cột computed ở SQL Server, chỉ cấu hình READ
    public string MemberName { get; } = string.Empty;

    public string Tier { get; set; } = "Bronze";
    public int TotalPoints { get; set; } = 0;
    public string? Avatar { get; set; }
    public DateTime? TierUpdatedAt { get; set; }
}
```

- `Robot.cs`:
```csharp
namespace SmartMarketBot.Domain.Entities;

public class Robot
{
    public int RobotID { get; set; }
    public string RobotName { get; set; } = string.Empty;
    public string RobotCode { get; set; } = string.Empty;
    public string? MacAddress { get; set; }
    public int BatteryPct { get; set; } = 100;
    public string Mode { get; set; } = "idle"; // idle, navigating, scanning, charging
    public bool IsOnline { get; set; } = false;
    public DateTime? LastSeenAt { get; set; }
}
```

---

### BƯỚC 3: INFRASTRUCTURE & DB CONTEXT SETUP (Định cấu hình Fluent API & SQL Views)
Trong dự án `SmartMarketBot.Infrastructure/Data/AppDbContext.cs`, hãy định cấu hình Fluent API cho tất cả 35 bảng thực thể.

#### 🌟 ĐẶC BIỆT: Ánh xạ 4 SQL Views làm Keyless Entity Types
Để EF Core có thể truy vấn trực tiếp 4 views tương thích ngược của n8n mà không ghi đè dữ liệu, hãy cấu hình chúng dưới dạng **Keyless Entities** (`HasNoKey()`) trỏ trực tiếp đến tên View trong cơ sở dữ liệu:
1. `PurchaseHistory` -> map tới View `PurchaseHistory`
2. `StoreMap` -> map tới View `Store_Map`
3. `BlockedAisle` -> map tới View `Blocked_Aisles`
4. `RealTimeStock` -> map tới View `Real_Time_Stock`

Ví dụ cấu hình Fluent API:
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Cấu hình table Members
    modelBuilder.Entity<Member>(entity =>
    {
        entity.ToTable("Members");
        entity.HasKey(e => e.MemberID);
        entity.Property(e => e.MemberName).HasComputedColumnSql("[FullName]", stored: false);
    });

    // Cấu hình table Robot_Logs
    modelBuilder.Entity<RobotLog>(entity =>
    {
        entity.ToTable("Robot_Logs");
        entity.HasKey(e => e.LogID);
    });

    // Cấu hình quan hệ tự liên kết sản phẩm thay thế
    modelBuilder.Entity<Product>()
        .HasOne(p => p.SubstituteProduct)
        .WithMany()
        .HasForeignKey(p => p.SubstituteProductID)
        .OnDelete(DeleteBehavior.NoAction);

    // MAPPING SQL VIEWS (Chỉ đọc - Keyless Entities)
    modelBuilder.Entity<StoreMapView>(entity =>
    {
        entity.HasNoKey();
        entity.ToView("Store_Map");
    });
    
    modelBuilder.Entity<PurchaseHistoryView>(entity =>
    {
        entity.HasNoKey();
        entity.ToView("PurchaseHistory");
    });
}
```

---

### BƯỚC 4: THỰC THI THUẬT TOÁN ĐIỀU HƯỚNG ROBOT (Dijkstra Algorithm)
Trong dự án `SmartMarketBot.Application/Services/NavigationService.cs`, hãy viết một service định vị và tìm đường. 
- Đầu vào: `fromNodeId` và `toNodeId`.
- Logic: Sử dụng thuật toán **Dijkstra** dựa trên bảng `NavigationNodes` và `NavigationEdges` để tìm danh sách các nút robot cần đi qua kèm theo toạ độ (X, Y) và khoảng cách thực tế.
- Trả về: Một DTO `RoutePlanResultDto` chứa danh sách Node đi qua theo đúng thứ tự, tổng khoảng cách, và danh sách toạ độ (x, y) để gửi cho Robot / Tablet.

---

### BƯỚC 5: XÂY DỰNG API CONTROLLERS
Hãy viết đầy đủ các Controller trong dự án `SmartMarketBot.API/Controllers/` phục vụ các tính năng:
1. **AuthController**: Đăng ký, đăng nhập người dùng, cấp Token JWT.
2. **ProductsController**: Tìm kiếm sản phẩm, lấy thông tin theo barcode, lấy sản phẩm liên quan dinh dưỡng tốt cho sức khỏe (lọc theo dị ứng của thành viên).
3. **NavigationController**:
   - `/api/navigation/route`: Tìm lộ trình ngắn nhất giữa 2 Node (gọi Dijkstra).
   - `/api/navigation/find-product/{productId}`: Tìm xem sản phẩm nằm ở Kệ nào (truy vấn Slot/Aisle), tìm Node dẫn đường gần nhất tới kệ đó, và vẽ đường từ vị trí robot hiện tại tới kệ đó.
4. **RobotsController**:
   - Nhận telemetry của robot gửi lên qua API (hoặc nhận MQTT rồi đẩy websocket SignalR).
   - `/api/robots/{robotCode}/command`: Gửi lệnh điều hành robot đi tới Node chỉ định (`go_to_node`).
5. **ShelfScansController**: 
   - Nhận ảnh chụp từ camera robot gửi lên, lưu trữ giả lập (hoặc kết nối Azure Blob), sau đó gửi HTTP POST tới AI Service FastAPI (`POST /api/shelf/analyze`) để nhận kết quả phân tích % trống, và lưu vào bảng `ShelfScans`. Nếu trống > 30%, kích hoạt SignalR cảnh báo hết hàng cho nhân viên.

---

### BƯỚC 6: THIẾT LẬP KẾT NỐI MQTT (MQTTnet Service)
Trong dự án `SmartMarketBot.Infrastructure/Mqtt/MqttClientService.cs`, hãy tạo một background service (`IHostedService`) tự động khởi chạy cùng ứng dụng:
- Kết nối tới MQTT Broker (Mosquitto local dev hoặc Azure IoT Hub).
- Subscribe vào topic `smartmarketbot/robot/+/status` và `smartmarketbot/robot/+/telemetry`.
- Khi nhận được tin nhắn từ Robot: Parse dữ liệu JSON, tự động ghi lịch sử nhật ký vào bảng `Robot_Logs`, đồng thời phát gói SignalR broadcast lên `RobotHub` để cập nhật trực tiếp toạ độ robot trên web dashboard hành trình.
- Cung cấp phương thức `PublishCommandAsync(string robotCode, object commandPayload)` để controllers có thể gọi bắn lệnh điều hướng xuống robot qua MQTT topic `smartmarketbot/robot/{robotCode}/command`.

---

### BƯỚC 7: CUNG CẤP CẤU HÌNH DOCKER COMPOSE
Tạo file `docker-compose.yml` ở thư mục gốc của giải pháp để khởi động nhanh môi trường phát triển local bao gồm:
1. **SQL Server 2022** (Chạy trên cổng 1433)
2. **Eclipse Mosquitto MQTT Broker** (Chạy trên cổng 1883)

---

Vui lòng tạo cấu trúc thư mục sạch đẹp, viết code tối ưu, xử lý bất đồng bộ (async/await) toàn bộ, xử lý lỗi (exception handling) chặt chẽ bằng Global Exception Middleware và đảm bảo ứng dụng có thể build chạy được ngay!
```

---

### Hướng dẫn cách làm việc với Cursor:
1. Mở thư mục dự án `SuperMarketBot-BE` bằng Cursor.
2. Sao chép toàn bộ khối lệnh Markdown bên trên (từ dòng `Bạn là một Kiến trúc sư...` đến dòng cuối cùng).
3. Mở **Composer** (bằng cách ấn tổ hợp phím `Ctrl + I` hoặc `Cmd + I` trên macOS).
4. Dán nội dung prompt vào ô nhập liệu và ấn **Enter**.
5. Cursor sẽ phân tích sơ đồ `db/database.sql` bạn vừa tạo và tự động sinh toàn bộ mã nguồn Clean Architecture hoàn hảo cho bạn! 🚀
