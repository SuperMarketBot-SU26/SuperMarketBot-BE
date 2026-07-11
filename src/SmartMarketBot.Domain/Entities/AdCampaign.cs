namespace SmartMarketBot.Domain.Entities;

public sealed class AdCampaign
{
    public int AdCampaignId { get; set; }
    public int PackageId { get; set; }
    public int BrandId { get; set; }
    public int? SemanticObjectId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = CampaignStatus.Inactive;

    /// <summary>Snapshot giá tại lúc brand mua shelf (SemanticObject). Charge mỗi impression = ShelfPriceCharged.</summary>
    public decimal ShelfPriceCharged { get; set; }

    public DateTime? ShelfPurchasedAt { get; set; }

    public AdPackage? Package { get; set; }
    public Brand? Brand { get; set; }
    public SemanticObject? SemanticObject { get; set; }
    public ICollection<AdCampaignLog> AdCampaignLogs { get; set; } = new List<AdCampaignLog>();
    public ICollection<SponsoredProduct> SponsoredProducts { get; set; } = new List<SponsoredProduct>();
    public ICollection<AdResource> AdResources { get; set; } = new List<AdResource>();
    public ICollection<AdCampaignZone> AdCampaignZones { get; set; } = new List<AdCampaignZone>();
    public ICollection<AdCampaignRoute> AdCampaignRoutes { get; set; } = new List<AdCampaignRoute>();
}

public static class CampaignStatus
{
    public const string Inactive = "Inactive";
    public const string Active = "Active";
    public const string Paused = "Paused";
    public const string Completed = "Completed";
    public const string Canceled = "Canceled";
}
