namespace SmartMarketBot.Domain.Entities;

public class NavigationEdge
{
    public int EdgeID { get; set; }
    public int FromNodeID { get; set; }
    public int ToNodeID { get; set; }
    public double Distance { get; set; }
    public bool IsBidirectional { get; set; } = true;

    public virtual NavigationNode FromNode { get; set; } = null!;
    public virtual NavigationNode ToNode { get; set; } = null!;
}
