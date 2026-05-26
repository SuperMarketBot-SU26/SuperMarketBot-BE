namespace SmartMarketBot.Domain.Entities;

public class Category
{
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }

    public virtual ICollection<Subcategory> Subcategories { get; set; } = new List<Subcategory>();
}
