namespace SmartMarketBot.Domain.Entities;

public class Product
{
    public int ProductID { get; set; }
    public int ProductTypeID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; } = 0.00m;
    public string? Barcode { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? WeightOrVolume { get; set; }
    public string? Unit { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int? SubstituteProductID { get; set; }

    public virtual ProductType ProductType { get; set; } = null!;
    public virtual Product? SubstituteProduct { get; set; }
    public virtual ICollection<Slot> Slots { get; set; } = new List<Slot>();
    public virtual ICollection<ProductHealthTag> ProductHealthTags { get; set; } = new List<ProductHealthTag>();
    public virtual ICollection<HistoryItem> HistoryItems { get; set; } = new List<HistoryItem>();
    public virtual ICollection<RecipeItem> RecipeItems { get; set; } = new List<RecipeItem>();
    public virtual ICollection<PromotionProduct> PromotionProducts { get; set; } = new List<PromotionProduct>();
    public virtual ICollection<SponsoredProduct> SponsoredProducts { get; set; } = new List<SponsoredProduct>();
}
