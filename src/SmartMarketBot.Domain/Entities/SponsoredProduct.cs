namespace SmartMarketBot.Domain.Entities;

public sealed class SponsoredProduct
{
    public int SponsoredId { get; set; }
    public int AdCampaignId { get; set; }
    public int ProductId { get; set; }
    public int Priority { get; set; }
    public string Status { get; set; } = SponsoredProductStatus.Active;
    
    public AdCampaign? AdCampaign { get; set; }
    public Product? Product { get; set; }
}

public static class SponsoredProductStatus
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
    public const string OutOfStock = "OutOfStock";
}
