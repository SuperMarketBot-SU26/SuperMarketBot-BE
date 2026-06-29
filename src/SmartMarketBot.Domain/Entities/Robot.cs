namespace SmartMarketBot.Domain.Entities;

public class Robot
{
    public int RobotId { get; set; }
    public string RobotName { get; set; } = string.Empty;
    public string RobotCode { get; set; } = string.Empty;
    public int BatteryPct { get; set; } = 100;
    public string Mode { get; set; } = "idle";

    /// <summary>Enum dạng string: 'Online' | 'Offline' | 'Maintenance' | 'Error'.</summary>
    public string Status { get; set; } = "Offline";

    public DateTime? LastSeenAt { get; set; }
    public string? IPAddress { get; set; }

    public virtual ICollection<RobotLog> RobotLogs { get; set; } = new List<RobotLog>();
    public virtual ICollection<RobotRoute> RobotRoutes { get; set; } = new List<RobotRoute>();
    public virtual ICollection<RouteAssignment> RouteAssignments { get; set; } = new List<RouteAssignment>();
    public virtual ICollection<AisleScan> AisleScans { get; set; } = new List<AisleScan>();
}
