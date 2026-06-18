namespace SmartMarketBot.Domain.Entities;

public class Brand
{
    public int BrandId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public decimal Wallet { get; set; } = 0.00m;
    public string? Description { get; set; }

    public virtual ICollection<AdCampaign> AdCampaigns { get; set; } = new List<AdCampaign>();
}
