namespace SmartMarketBot.Domain.Entities;

public class AdRouteCampaign
{
    public int AdRouteId { get; set; }
    public int AdCampaignId { get; set; }

    public virtual AdRoute? AdRoute { get; set; }
    public virtual AdCampaign? AdCampaign { get; set; }
}
