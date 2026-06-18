namespace SmartMarketBot.Domain.Entities;

/// <summary>
/// Bảng lưu 2 loại sự kiện quảng cáo (đa mục đích, không tách bảng):
/// <list type="bullet">
///   <item><b>ActionType = "Click"</b> — khách bấm vào sản phẩm tài trợ (legacy).</item>
///   <item><b>ActionType = "View"</b>  — khách xem chi tiết (legacy).</item>
///   <item><b>ActionType = "RoutePass"</b> — Phase B: robot chạy qua Slot thuộc Zone có AdCampaign active, tính 1 impression Route-based. <b>Đây là nguồn billing chính thức</b>, không tính theo click.</item>
/// </list>
/// Phase B mở rộng thêm các cột nullable: <c>SponsoredId</c>, <c>ProductId</c>, <c>RobotId</c>, <c>RobotZoneId</c>, <c>ZoneId</c>, <c>SlotId</c>, <c>MemberId</c>, <c>XCoord</c>, <c>YCoord</c>.
/// </summary>
public class AdCampaignLog
{
    public int LogId { get; set; }
    public int AdCampaignId { get; set; }

    /// <summary>Enum dạng string: 'Click' | 'View' | 'RoutePass'.</summary>
    public string ActionType { get; set; } = string.Empty;

    public decimal ChargedAmount { get; set; } = 0.00m;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // ── Phase B: thông tin chi tiết cho RoutePass (nullable cho dữ liệu Click/View cũ) ──
    public int? SponsoredId { get; set; }
    public int? ProductId { get; set; }
    public int? RobotId { get; set; }
    public int? RobotZoneId { get; set; }
    public int? ZoneId { get; set; }
    public int? SlotId { get; set; }
    public int? MemberId { get; set; }
    public int? XCoord { get; set; }
    public int? YCoord { get; set; }

    public virtual AdCampaign? AdCampaign { get; set; }
    public virtual SponsoredProduct? SponsoredProduct { get; set; }
    public virtual Product? Product { get; set; }
    public virtual Robot? Robot { get; set; }
    public virtual RobotZone? RobotZone { get; set; }
    public virtual Zone? Zone { get; set; }
    public virtual Slot? Slot { get; set; }
    public virtual Member? Member { get; set; }
}
