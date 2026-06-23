namespace SmartMarketBot.Domain.Entities;

public class SemanticObject
{
    public int ObjectId { get; set; }
    public int MapId { get; set; }
    public string ObjectType { get; set; } = string.Empty;
    public double XMin { get; set; }
    public double YMin { get; set; }
    public double XMax { get; set; }
    public double YMax { get; set; }
    public string? Label { get; set; }
    public double? Confidence { get; set; }
    public DateTime? DetectedAt { get; set; }
    public string? ImageUrl { get; set; }
    public int? ProductId { get; set; }

    public virtual Map? Map { get; set; }
    public virtual Product? Product { get; set; }
}
