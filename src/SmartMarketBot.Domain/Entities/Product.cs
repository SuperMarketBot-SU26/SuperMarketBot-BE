namespace SmartMarketBot.Domain.Entities;

public class Product
{
    public int ProductId { get; set; }
    public int ProductTypeId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; } = 0.00m;
    public string? Barcode { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? WeightOrVolume { get; set; }
    public string? Unit { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int? SubstituteProductId { get; set; }

    public virtual ProductType? ProductType { get; set; }
    public virtual Product? SubstituteProduct { get; set; }
    public virtual ICollection<ProductHealthTag> ProductHealthTags { get; set; } = new List<ProductHealthTag>();
    public virtual ICollection<InvoiceHistoryItem> InvoiceHistoryItems { get; set; } = new List<InvoiceHistoryItem>();
    public virtual ICollection<MealItem> MealItems { get; set; } = new List<MealItem>();
    public virtual ICollection<ProductSlot> ProductSlots { get; set; } = new List<ProductSlot>();
    public virtual ICollection<SponsoredProduct> SponsoredProducts { get; set; } = new List<SponsoredProduct>();
}
