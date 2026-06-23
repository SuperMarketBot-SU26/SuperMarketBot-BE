namespace SmartMarketBot.Domain.Entities;

public class Map
{
    public int MapId { get; set; }
    public int FloorId { get; set; }
    public string MapName { get; set; } = string.Empty;
    public string? MapData { get; set; }
    public string? FloorplanImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Floor? Floor { get; set; }
    public virtual ICollection<NavigationNode> NavigationNodes { get; set; } = new List<NavigationNode>();
    public virtual ICollection<SemanticObject> SemanticObjects { get; set; } = new List<SemanticObject>();
    public virtual ICollection<RobotRoute> RobotRoutes { get; set; } = new List<RobotRoute>();
}
