namespace SmartMarketBot.Domain.Entities;

public class HistoryItem
{
    public int HistoryItemID { get; set; }
    public int ShoppingHistoryID { get; set; }
    public int ProductID { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; } = 0.00m;

    public virtual ShoppingHistory ShoppingHistory { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}
