namespace SmartMarketBot.Domain.Entities;

public class RobotAdRouteAssignment
{
    public int AssignmentId { get; set; }
    public int RobotId { get; set; }
    public int AdRouteId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Active"; // Active | Paused | Completed

    public virtual Robot? Robot { get; set; }
    public virtual AdRoute? AdRoute { get; set; }
}
