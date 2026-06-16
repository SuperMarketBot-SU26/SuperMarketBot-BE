namespace SmartMarketBot.Domain.Entities;

public class RobotZone
{
    public int RobotZoneId { get; set; }
    public int RobotId { get; set; }
    public int ZoneId { get; set; }

    public virtual Robot? Robot { get; set; }
    public virtual Zone? Zone { get; set; }
}
