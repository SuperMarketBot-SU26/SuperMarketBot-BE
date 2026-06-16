namespace SmartMarketBot.Domain.Entities;

public class RouteAssignment
{
    public int RouteAssignmentId { get; set; }
    public int RobotId { get; set; }
    public int RobotRouteId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";

    public virtual Robot? Robot { get; set; }
    public virtual RobotRoute? RobotRoute { get; set; }
}
