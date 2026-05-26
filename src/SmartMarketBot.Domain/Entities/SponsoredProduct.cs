namespace SmartMarketBot.Domain.Entities;

public class SponsoredProduct
{
    public int SponsoredID { get; set; }
    public int ProductID { get; set; }
    public string SponsorBrand { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int Priority { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public virtual Product Product { get; set; } = null!;
}
