namespace SmartMarketBot.Domain.Entities;

public class AdRoute
{
    public int AdRouteId { get; set; }
    public string RouteName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<AdRouteNode> Nodes { get; set; } = new List<AdRouteNode>();
    public virtual ICollection<AdRouteCampaign> Campaigns { get; set; } = new List<AdRouteCampaign>();
}
