namespace SmartMarketBot.Domain.Entities;

public class PromotionProduct
{
    public int PromotionID { get; set; }
    public int ProductID { get; set; }
    public int Priority { get; set; } = 0;

    public virtual Promotion Promotion { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}
