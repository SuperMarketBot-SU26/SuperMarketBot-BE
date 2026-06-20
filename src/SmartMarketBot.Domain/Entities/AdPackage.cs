namespace SmartMarketBot.Domain.Entities;

public sealed class AdPackage
{
    public int PackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public decimal PricePackage { get; set; }
    public decimal PriceRoute { get; set; }
    public decimal BasePriceClick { get; set; }
    public int AdScore { get; set; }
    public string Status { get; set; } = "Active";
    
    public ICollection<AdCampaign> AdCampaigns { get; set; } = new List<AdCampaign>();
}
