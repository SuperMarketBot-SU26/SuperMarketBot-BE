namespace SmartMarketBot.Domain.Entities;

public class AisleNode
{
    public int AisleNodeId { get; set; }
    public int AisleId { get; set; }
    public int NodeId { get; set; }

    public virtual Aisle? Aisle { get; set; }
    public virtual NavigationNode? Node { get; set; }
}
