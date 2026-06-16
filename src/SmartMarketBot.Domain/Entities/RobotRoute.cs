namespace SmartMarketBot.Domain.Entities;

public class RobotRoute
{
    public int RobotRouteId { get; set; }
    public int RobotId { get; set; }
    public int MapId { get; set; }
    public string RouteName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Robot? Robot { get; set; }
    public virtual Map? Map { get; set; }
    public virtual ICollection<RouteNodeMapping> RouteNodeMappings { get; set; } = new List<RouteNodeMapping>();
    public virtual ICollection<RouteAssignment> RouteAssignments { get; set; } = new List<RouteAssignment>();
}
