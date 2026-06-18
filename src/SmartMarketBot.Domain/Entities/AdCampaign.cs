namespace SmartMarketBot.Domain.Entities;

public class AdCampaign
{
    public int AdCampaignId { get; set; }
    public int PackageId { get; set; }
    public int BrandId { get; set; }
    public int? RobotZoneId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    /// <summary>Enum dạng string: 'Scheduled' | 'Running' | 'Paused' | 'Completed' | 'Cancelled'.</summary>
    public string Status { get; set; } = "Scheduled";

    public virtual AdPackage? Package { get; set; }
    public virtual Brand? Brand { get; set; }
    public virtual RobotZone? RobotZone { get; set; }
    public virtual ICollection<SponsoredProduct> SponsoredProducts { get; set; } = new List<SponsoredProduct>();
    public virtual ICollection<AdCampaignLog> AdCampaignLogs { get; set; } = new List<AdCampaignLog>();
}
