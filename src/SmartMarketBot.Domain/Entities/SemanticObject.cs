using SmartMarketBot.Domain.Enums;

namespace SmartMarketBot.Domain.Entities;

public class SemanticObject
{
    public int ObjectId { get; set; }
    public int MapId { get; set; }
    public SemanticObjectType ObjectType { get; set; }
    public double XMin { get; set; }
    public double YMin { get; set; }
    public double XMax { get; set; }
    public double YMax { get; set; }
    public string? Label { get; set; }
    public double? Confidence { get; set; }
    public DateTime? DetectedAt { get; set; }
    public string? ImageUrl { get; set; }
    public int? ProductTypeId { get; set; }

    public virtual Map? Map { get; set; }
    public virtual ProductType? ProductType { get; set; }
}
