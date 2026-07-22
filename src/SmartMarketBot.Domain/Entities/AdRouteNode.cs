namespace SmartMarketBot.Domain.Entities;

public class AdRouteNode
{
    public int AdRouteNodeId { get; set; }
    public int AdRouteId { get; set; }
    public int NodeId { get; set; }
    public int SequenceOrder { get; set; }
    public int DwellTimeSeconds { get; set; } = 30;

    /// <summary>
    /// ZoneId of the aisle this node belongs to.
    /// Used to group nodes into playlists for Autonomous mode.
    /// </summary>
    public int? ZoneId { get; set; }

    public virtual AdRoute? AdRoute { get; set; }
    public virtual NavigationNode? Node { get; set; }
    public virtual Zone? Zone { get; set; }
}
