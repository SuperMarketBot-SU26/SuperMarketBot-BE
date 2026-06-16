namespace SmartMarketBot.Domain.Entities;

public class MealItem
{
    public int MealSuggestionId { get; set; }
    public int ProductId { get; set; }
    public decimal QuantityRequired { get; set; }
    public string UnitOfMeasure { get; set; } = "g";

    public virtual MealSuggestion? MealSuggestion { get; set; }
    public virtual Product? Product { get; set; }
}
