namespace SmartMarketBot.Domain.Entities;

public class Brand
{
    public int BrandID { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string? Description { get; set; }

    public virtual ICollection<SponsoredProduct> SponsoredProducts { get; set; } = new List<SponsoredProduct>();
}
