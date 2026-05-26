namespace SmartMarketBot.Domain.Entities;

public class Workstation
{
    public int WorkstationID { get; set; }
    public int ZoneID { get; set; }
    public int NodeID { get; set; }
    public string StationName { get; set; } = string.Empty;

    public virtual Zone Zone { get; set; } = null!;
    public virtual NavigationNode Node { get; set; } = null!;
}
