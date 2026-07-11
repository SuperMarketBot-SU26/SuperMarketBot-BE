namespace SmartMarketBot.Domain.Entities;

public class RobotRoute
{
    public int RobotRouteId { get; set; }
    public int RobotId { get; set; }
    public int MapId { get; set; }
    public string RouteName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // === Phase 1: phân loại & liên kết khu vực ===
    /// <summary>Liên kết khu vực (Zone) để lọc / phân loại lộ trình theo khu vực bản đồ. Null = chưa gán.</summary>
    public int? ZoneId { get; set; }
    /// <summary>Loại lộ trình: patrol | restock | delivery | custom. Mặc định 'patrol' (tuần tra quét kệ).</summary>
    public string RouteType { get; set; } = "patrol";
    /// <summary>Mô tả ngắn cho admin (vd: "Tuần tra khu rau củ, quét kệ 5-12 mỗi sáng").</summary>
    public string? Description { get; set; }

    public virtual Robot? Robot { get; set; }
    public virtual Map? Map { get; set; }
    public virtual Zone? Zone { get; set; }
    public virtual ICollection<RouteNodeMapping> RouteNodeMappings { get; set; } = new List<RouteNodeMapping>();
    public virtual ICollection<RouteAssignment> RouteAssignments { get; set; } = new List<RouteAssignment>();
    public virtual ICollection<AdCampaignRoute> AdCampaignRoutes { get; set; } = new List<AdCampaignRoute>();
}
