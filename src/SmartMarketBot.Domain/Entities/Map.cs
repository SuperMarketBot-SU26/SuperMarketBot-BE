using SmartMarketBot.Domain.Common;

namespace SmartMarketBot.Domain.Entities;

public class Map
{
    public int MapID { get; set; }
    public int FloorID { get; set; }
    public string MapName { get; set; } = string.Empty;
    public string? MapData { get; set; }
    public DateTime CreatedAt { get; set; } = VnDateTime.Now;

    public virtual Floor Floor { get; set; } = null!;
    public virtual ICollection<NavigationNode> NavigationNodes { get; set; } = new List<NavigationNode>();
    public virtual ICollection<SemanticObject> SemanticObjects { get; set; } = new List<SemanticObject>();
}
