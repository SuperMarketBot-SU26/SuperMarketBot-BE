namespace SmartMarketBot.Domain.Entities;

public class Robot
{
    public int RobotID { get; set; }
    public string RobotName { get; set; } = string.Empty;
    public string RobotCode { get; set; } = string.Empty;
    public string? MacAddress { get; set; }
    public int BatteryPct { get; set; } = 100;
    public string Mode { get; set; } = "idle";
    public bool IsOnline { get; set; } = false;
    public DateTime? LastSeenAt { get; set; }

    public virtual ICollection<RobotZone> RobotZones { get; set; } = new List<RobotZone>();
    public virtual ICollection<RobotLog> RobotLogs { get; set; } = new List<RobotLog>();
    public virtual ICollection<ShelfScan> ShelfScans { get; set; } = new List<ShelfScan>();
}
