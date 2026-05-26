namespace SmartMarketBot.Domain.Entities;

public class RecipeItem
{
    public int RecipeID { get; set; }
    public int ProductID { get; set; }
    public decimal QuantityRequired { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;

    public virtual Recipe Recipe { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}
