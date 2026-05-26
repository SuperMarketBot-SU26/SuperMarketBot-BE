namespace SmartMarketBot.Domain.Entities;

public class Aisle
{
    public int AisleID { get; set; }
    public int ZoneID { get; set; }
    public string AisleCode { get; set; } = string.Empty;
    public string? AisleName { get; set; }
    public bool IsBlocked { get; set; } = false;
    public string? BlockReason { get; set; }

    public virtual Zone Zone { get; set; } = null!;
    public virtual ICollection<ShelfLevel> ShelfLevels { get; set; } = new List<ShelfLevel>();
    public virtual ICollection<ShelfScan> ShelfScans { get; set; } = new List<ShelfScan>();
    public virtual ICollection<NavigationNode> NavigationNodes { get; set; } = new List<NavigationNode>();
}
