namespace SmartMarketBot.Domain.Entities;

public class AdRoute
{
    public int AdRouteId { get; set; }
    public string RouteName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// If true: Autonomous mode (sequential playlist from stops)
    /// If false: Zone/Shelf mode (AABB spatial detection)
    /// </summary>
    public bool IsAutonomous { get; set; } = false;

    /// <summary>
    /// For Zone/Shelf mode: target SemanticObject to track.
    /// When robot enters this object's AABB, broadcast its ads.
    /// Null = use all campaigns linked via AdRouteCampaign.
    /// </summary>
    public int? SemanticObjectId { get; set; }

    public virtual SemanticObject? SemanticObject { get; set; }
    public virtual ICollection<AdRouteNode> Nodes { get; set; } = new List<AdRouteNode>();
    public virtual ICollection<AdRouteCampaign> Campaigns { get; set; } = new List<AdRouteCampaign>();
    public virtual ICollection<RobotAdRouteAssignment> Assignments { get; set; } = new List<RobotAdRouteAssignment>();
}
