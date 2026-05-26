namespace SmartMarketBot.Domain.Entities;

public class ProductHealthTag
{
    public int ProductID { get; set; }
    public int TagID { get; set; }

    public virtual Product Product { get; set; } = null!;
    public virtual HealthTag HealthTag { get; set; } = null!;
}
