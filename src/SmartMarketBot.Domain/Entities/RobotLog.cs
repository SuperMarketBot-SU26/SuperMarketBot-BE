namespace SmartMarketBot.Domain.Entities;

public class RobotLog
{
    public int LogId { get; set; }
    public int? RobotId { get; set; }
    public int? Battery { get; set; }
    public string? Location { get; set; }

    /// <summary>Enum dạng string: 'Idle' | 'Navigating' | 'Scanning' | 'Charging' | 'Error' | 'Offline'.</summary>
    public string Status { get; set; } = "Idle";

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public double? XCoord { get; set; }
    public double? YCoord { get; set; }
    public double? HeadingRad { get; set; }

    /// <summary>
    /// Node ID trên bản đồ (NAVIGATION_NODE) mà robot đang đứng/kế tiếp.
    /// P0-3/4 FIX: dùng để auto-dock và reroute chọn đúng startNode thay vì RobotId.
    /// Nullable để không phá vỡ dữ liệu log cũ (cột sẽ NULL với record trước migration).
    /// </summary>
    public int? CurrentNodeId { get; set; }

    /// <summary>
    /// Phase B Step 2 — Mã vật lý tại waypoint (RFID/QR/tape-line) do firmware line-scan đọc.
    /// Line-scan navigation chỉ gửi NodeCode về BE — BE lưu raw vào log để debug trajectory
    /// và phục vụ Phase 4 (ad engine: truy node → semantic-object qua NODE_SEMANTIC_OBJECT).
    /// Legacy AMR vẫn chỉ gửi X/Y/CurrentNodeId; trường này NULL cho log cũ.
    /// </summary>
    public string? CurrentNodeCode { get; set; }

    public virtual Robot? Robot { get; set; }
}
