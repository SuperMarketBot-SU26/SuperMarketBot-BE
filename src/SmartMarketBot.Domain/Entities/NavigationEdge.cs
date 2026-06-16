namespace SmartMarketBot.Domain.Entities;

public class NavigationEdge
{
    public int EdgeId { get; set; }
    public int FromNodeId { get; set; }
    public int ToNodeId { get; set; }
    public double Distance { get; set; }
    public bool IsBidirectional { get; set; } = true;

    public virtual NavigationNode? FromNode { get; set; }
    public virtual NavigationNode? ToNode { get; set; }
}
