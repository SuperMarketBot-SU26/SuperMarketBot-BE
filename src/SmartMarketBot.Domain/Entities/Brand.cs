namespace SmartMarketBot.Domain.Entities;

public sealed class Brand
{
    public int BrandId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public decimal Wallet { get; set; }
    public string? Description { get; set; }
    
    public ICollection<AdCampaign> AdCampaigns { get; set; } = new List<AdCampaign>();
}
