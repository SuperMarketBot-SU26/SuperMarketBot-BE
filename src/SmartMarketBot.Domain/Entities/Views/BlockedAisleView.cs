namespace SmartMarketBot.Domain.Entities.Views;

public class BlockedAisleView
{
    public int AisleID { get; set; }
    public string AisleCode { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
    public string? Reason { get; set; }
}
