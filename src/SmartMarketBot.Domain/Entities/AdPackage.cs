namespace SmartMarketBot.Domain.Entities;

public sealed class AdPackage
{
    public int PackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;

    /// <summary>Phí cố định / 1 campaign (charge 1 lần lúc activate).</summary>
    public decimal PricePackage { get; set; }

    /// <summary>Đơn giá / 1 route. Charge = PriceRoute × số route mua.</summary>
    public decimal PriceRoute { get; set; }

    /// <summary>Đơn giá / 1 zone. Charge = PriceZone × số zone mua.</summary>
    public decimal PriceZone { get; set; }

    /// <summary>Đơn giá / 1 shelf (SemanticObject). Charge = PriceShelf × 1 nếu có SemanticObjectId.</summary>
    public decimal PriceShelf { get; set; }

    public decimal BasePriceClick { get; set; }
    public int AdScore { get; set; }
    public string Status { get; set; } = "Active";

    public ICollection<AdCampaign> AdCampaigns { get; set; } = new List<AdCampaign>();
}
