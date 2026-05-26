namespace SmartMarketBot.Domain.Entities;

public class NavigationNode
{
    public int NodeID { get; set; }
    public int MapID { get; set; }
    public string NodeName { get; set; } = string.Empty;
    public double XCoord { get; set; }
    public double YCoord { get; set; }
    public string NodeType { get; set; } = string.Empty;
    public int? LinkedAisleID { get; set; }
    public bool IsBlocked { get; set; } = false;

    public virtual Map Map { get; set; } = null!;
    public virtual Aisle? LinkedAisle { get; set; }
    public virtual ICollection<NavigationEdge> OutgoingEdges { get; set; } = new List<NavigationEdge>();
    public virtual ICollection<NavigationEdge> IncomingEdges { get; set; } = new List<NavigationEdge>();
    public virtual ICollection<Workstation> Workstations { get; set; } = new List<Workstation>();
    public virtual ICollection<RobotLog> RobotLogs { get; set; } = new List<RobotLog>();
}
