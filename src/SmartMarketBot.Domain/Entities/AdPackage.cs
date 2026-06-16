namespace SmartMarketBot.Domain.Entities;

public class AdPackage
{
    public int PackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public decimal Price { get; set; } = 0.00m;
    public int AdScore { get; set; } = 0;
    public bool IsWeekendOnly { get; set; } = false;

    public virtual ICollection<AdCampaign> AdCampaigns { get; set; } = new List<AdCampaign>();
}
