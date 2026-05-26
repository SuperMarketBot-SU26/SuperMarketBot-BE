namespace SmartMarketBot.Domain.Entities;

public class ProductType
{
    public int ProductTypeID { get; set; }
    public int SubcategoryID { get; set; }
    public string ProductTypeName { get; set; } = string.Empty;

    public virtual Subcategory Subcategory { get; set; } = null!;
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
