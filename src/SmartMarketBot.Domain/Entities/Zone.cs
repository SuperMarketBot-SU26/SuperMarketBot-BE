namespace SmartMarketBot.Domain.Entities;

public class Zone
{
    public int ZoneId { get; set; }
    public int FloorId { get; set; }
    public string ZoneCode { get; set; } = string.Empty;
    public string ZoneName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsBlocked { get; set; } = false;

    public virtual Floor? Floor { get; set; }
    public virtual ICollection<Aisle> Aisles { get; set; } = new List<Aisle>();
    public virtual ICollection<RobotZone> RobotZones { get; set; } = new List<RobotZone>();
}
