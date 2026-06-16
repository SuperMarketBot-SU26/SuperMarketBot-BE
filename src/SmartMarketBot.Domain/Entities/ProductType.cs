namespace SmartMarketBot.Domain.Entities;

public class ProductType
{
    public int ProductTypeId { get; set; }
    public int SubcategoryId { get; set; }
    public string TypeName { get; set; } = string.Empty;

    public virtual Subcategory? Subcategory { get; set; }
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
