namespace SmartMarketBot.Domain.Entities;

public class AdCampaignZone
{
    public int AdCampaignId { get; set; }
    public int ZoneId { get; set; }

    /// <summary>Snapshot giá tại lúc brand mua zone. Charge mỗi impression = ZonePriceCharged.</summary>
    public decimal ZonePriceCharged { get; set; }

    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;

    public virtual AdCampaign? AdCampaign { get; set; }
    public virtual Zone? Zone { get; set; }
}
