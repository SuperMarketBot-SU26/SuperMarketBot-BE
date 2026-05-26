namespace SmartMarketBot.Domain.Entities;

public class Promotion
{
    public int PromotionID { get; set; }
    public string PromotionName { get; set; } = string.Empty;
    public string PromotionType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; } = 0.00m;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ICollection<PromotionProduct> PromotionProducts { get; set; } = new List<PromotionProduct>();
}
