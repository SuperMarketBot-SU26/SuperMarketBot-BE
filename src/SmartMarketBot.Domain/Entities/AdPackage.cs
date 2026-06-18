namespace SmartMarketBot.Domain.Entities;

public class AdPackage
{
    public int PackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public decimal PricePackage { get; set; } = 0.00m;
    public decimal PriceRoute { get; set; } = 0.00m;
    public decimal BasePriceClick { get; set; } = 0.00m;
    public int AdScore { get; set; } = 0;

    /// <summary>Enum dạng string: 'Active' | 'Inactive'.</summary>
    public string Status { get; set; } = "Active";

    public virtual ICollection<AdCampaign> AdCampaigns { get; set; } = new List<AdCampaign>();
}
