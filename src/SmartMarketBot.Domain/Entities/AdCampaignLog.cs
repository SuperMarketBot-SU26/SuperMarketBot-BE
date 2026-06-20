namespace SmartMarketBot.Domain.Entities;

public sealed class AdCampaignLog
{
    public int LogId { get; set; }
    public int AdCampaignId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public decimal ChargedAmount { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int? SponsoredId { get; set; }
    public int? ProductId { get; set; }
    public int? RobotId { get; set; }
    public int? RobotZoneId { get; set; }
    public int? ZoneId { get; set; }
    public int? SlotId { get; set; }
    public int? MemberId { get; set; }
    public decimal? XCoord { get; set; }
    public decimal? YCoord { get; set; }
    public string? SessionId { get; set; }
    
    public AdCampaign? AdCampaign { get; set; }
    public SponsoredProduct? SponsoredProduct { get; set; }
    public Product? Product { get; set; }
    public Robot? Robot { get; set; }
    public RobotZone? RobotZone { get; set; }
    public Zone? Zone { get; set; }
    public Slot? Slot { get; set; }
    public Member? Member { get; set; }
}

public static class AdActionType
{
    public const string Click = "Click";
    public const string Navigation = "Navigation";
    public const string Impression = "Impression";
    public const string FraudDetected = "FraudDetected";
}
