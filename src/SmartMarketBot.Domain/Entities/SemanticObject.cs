namespace SmartMarketBot.Domain.Entities;

public class SemanticObject
{
    public int ObjectID { get; set; }
    public int MapID { get; set; }
    public string ObjectType { get; set; } = string.Empty;
    public double XMin { get; set; }
    public double YMin { get; set; }
    public double XMax { get; set; }
    public double YMax { get; set; }

    public virtual Map Map { get; set; } = null!;
}
