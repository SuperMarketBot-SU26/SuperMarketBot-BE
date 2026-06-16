namespace SmartMarketBot.Domain.Entities;

public class Subcategory
{
    public int SubcategoryId { get; set; }
    public int CategoryId { get; set; }
    public string SubcategoryName { get; set; } = string.Empty;

    public virtual Category? Category { get; set; }
    public virtual ICollection<ProductType> ProductTypes { get; set; } = new List<ProductType>();
}
