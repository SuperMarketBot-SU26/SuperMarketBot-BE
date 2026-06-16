namespace SmartMarketBot.Domain.Entities;

public class SponsoredProduct
{
    public int SponsoredId { get; set; }
    public int AdCampaignId { get; set; }
    public int ProductId { get; set; }
    public int BrandId { get; set; }
    public int Priority { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public virtual AdCampaign? AdCampaign { get; set; }
    public virtual Product? Product { get; set; }
    public virtual Brand? Brand { get; set; }
}
