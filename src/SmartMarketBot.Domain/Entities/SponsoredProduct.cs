namespace SmartMarketBot.Domain.Entities;

public class SponsoredProduct
{
    public int SponsoredId { get; set; }
    public int AdCampaignId { get; set; }
    public int ProductId { get; set; }
    public int Priority { get; set; } = 0;

    /// <summary>Enum dạng string: 'Active' | 'Paused' | 'Expired'.</summary>
    public string Status { get; set; } = "Active";

    public virtual AdCampaign? AdCampaign { get; set; }
    public virtual Product? Product { get; set; }
}
