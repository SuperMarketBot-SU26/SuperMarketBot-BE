namespace SmartMarketBot.Domain.Entities;

public sealed class AdCampaign
{
    public int AdCampaignId { get; set; }
    public int PackageId { get; set; }
    public int BrandId { get; set; }
    public int? RobotZoneId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = CampaignStatus.Inactive;

    public AdPackage? Package { get; set; }
    public Brand? Brand { get; set; }
    public RobotZone? RobotZone { get; set; }
    public ICollection<AdCampaignLog> AdCampaignLogs { get; set; } = new List<AdCampaignLog>();
    public ICollection<SponsoredProduct> SponsoredProducts { get; set; } = new List<SponsoredProduct>();
    public ICollection<AdResource> AdResources { get; set; } = new List<AdResource>();
}

public static class CampaignStatus
{
    public const string Inactive = "Inactive";
    public const string Active = "Active";
    public const string Paused = "Paused";
    public const string Completed = "Completed";
    public const string Canceled = "Canceled";
}
