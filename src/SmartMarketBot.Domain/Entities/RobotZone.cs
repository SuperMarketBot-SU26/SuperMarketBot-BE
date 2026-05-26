namespace SmartMarketBot.Domain.Entities;

public class RobotZone
{
    public int RobotID { get; set; }
    public int ZoneID { get; set; }

    public virtual Robot Robot { get; set; } = null!;
    public virtual Zone Zone { get; set; } = null!;
}
