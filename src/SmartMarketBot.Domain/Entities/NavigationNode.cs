namespace SmartMarketBot.Domain.Entities;

public class NavigationNode
{
    public int NodeId { get; set; }
    public int MapId { get; set; }
    public string NodeName { get; set; } = string.Empty;
    public double XCoord { get; set; }
    public double YCoord { get; set; }
    public string NodeType { get; set; } = "intersection";
    public bool IsBlocked { get; set; } = false;

    public virtual Map? Map { get; set; }
    public virtual ICollection<NavigationEdge> OutgoingEdges { get; set; } = new List<NavigationEdge>();
    public virtual ICollection<NavigationEdge> IncomingEdges { get; set; } = new List<NavigationEdge>();
    public virtual ICollection<AisleNode> AisleNodes { get; set; } = new List<AisleNode>();
    public virtual ICollection<RouteNodeMapping> RouteNodeMappings { get; set; } = new List<RouteNodeMapping>();
}
