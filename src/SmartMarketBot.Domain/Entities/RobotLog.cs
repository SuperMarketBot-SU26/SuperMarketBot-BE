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

    public virtual Robot? Robot { get; set; }
}
