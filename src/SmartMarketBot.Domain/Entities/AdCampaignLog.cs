namespace SmartMarketBot.Domain.Entities;

public class AdCampaignLog
{
    public int LogId { get; set; }
    public int AdCampaignId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public virtual AdCampaign? AdCampaign { get; set; }
}
