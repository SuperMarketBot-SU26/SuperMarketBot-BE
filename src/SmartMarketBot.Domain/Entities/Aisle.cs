namespace SmartMarketBot.Domain.Entities;

public class Aisle
{
    public int AisleId { get; set; }
    public int ZoneId { get; set; }
    public string AisleCode { get; set; } = string.Empty;
    public string? AisleName { get; set; }

    public virtual Zone? Zone { get; set; }
    public virtual ICollection<Shelf> Shelves { get; set; } = new List<Shelf>();
    public virtual ICollection<AisleNode> AisleNodes { get; set; } = new List<AisleNode>();
    public virtual ICollection<AisleScan> AisleScans { get; set; } = new List<AisleScan>();
}
