namespace SmartMarketBot.Domain.Entities;

public class RobotLog
{
    public long LogID { get; set; }
    public int? RobotID { get; set; }
    public int? battery { get; set; }
    public string? location { get; set; }
    public string? status { get; set; }
    public DateTime timestamp { get; set; } = DateTime.UtcNow;
    public int? CurrentNodeID { get; set; }
    public string? Mode { get; set; }
    public bool? IsOnline { get; set; }
    public double? XCoord { get; set; }
    public double? YCoord { get; set; }

    public virtual Robot? Robot { get; set; }
    public virtual NavigationNode? CurrentNode { get; set; }
}
