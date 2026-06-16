namespace SmartMarketBot.Domain.Entities;

public class InvoiceHistoryItem
{
    public int InvoiceHistoryItemId { get; set; }
    public int InvoiceHistoryId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; } = 0.00m;

    public virtual InvoiceHistory? InvoiceHistory { get; set; }
    public virtual Product? Product { get; set; }
}
