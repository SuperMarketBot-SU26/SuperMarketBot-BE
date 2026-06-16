namespace SmartMarketBot.Domain.Entities;

public class Brand
{
    public int BrandId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string? Description { get; set; }

    public virtual ICollection<AdCampaign> AdCampaigns { get; set; } = new List<AdCampaign>();
    public virtual ICollection<SponsoredProduct> SponsoredProducts { get; set; } = new List<SponsoredProduct>();
}
