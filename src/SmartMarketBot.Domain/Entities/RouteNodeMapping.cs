namespace SmartMarketBot.Domain.Entities;

public class RouteNodeMapping
{
    public int RouteNodeMappingId { get; set; }
    public int RobotRouteId { get; set; }
    public int NodeId { get; set; }
    public int SequenceOrder { get; set; }

    public virtual RobotRoute? RobotRoute { get; set; }
    public virtual NavigationNode? Node { get; set; }
}
