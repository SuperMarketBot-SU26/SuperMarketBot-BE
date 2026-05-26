namespace SmartMarketBot.Domain.Entities;

public class Subcategory
{
    public int SubcategoryID { get; set; }
    public int CategoryID { get; set; }
    public string SubcategoryName { get; set; } = string.Empty;

    public virtual Category Category { get; set; } = null!;
    public virtual ICollection<ProductType> ProductTypes { get; set; } = new List<ProductType>();
}
