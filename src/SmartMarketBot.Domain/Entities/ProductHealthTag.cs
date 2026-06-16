namespace SmartMarketBot.Domain.Entities;

public class ProductHealthTag
{
    public int ProductId { get; set; }
    public int HealthTagId { get; set; }

    public virtual Product? Product { get; set; }
    public virtual HealthTag? HealthTag { get; set; }
}
