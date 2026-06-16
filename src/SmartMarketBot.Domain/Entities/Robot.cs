namespace SmartMarketBot.Domain.Entities;

public class Robot
{
    public int RobotId { get; set; }
    public string RobotName { get; set; } = string.Empty;
    public string RobotCode { get; set; } = string.Empty;
    public int BatteryPct { get; set; } = 100;
    public string Mode { get; set; } = "idle";
    public bool IsOnline { get; set; } = false;
    public DateTime? LastSeenAt { get; set; }

    public virtual ICollection<RobotLog> RobotLogs { get; set; } = new List<RobotLog>();
    public virtual ICollection<RobotZone> RobotZones { get; set; } = new List<RobotZone>();
    public virtual ICollection<RobotRoute> RobotRoutes { get; set; } = new List<RobotRoute>();
    public virtual ICollection<RouteAssignment> RouteAssignments { get; set; } = new List<RouteAssignment>();
    public virtual ICollection<AisleScan> AisleScans { get; set; } = new List<AisleScan>();
}
