namespace SmartMarketBot.Domain.Entities;

public class InvoiceHistory
{
    public int InvoiceHistoryId { get; set; }
    public int MemberId { get; set; }
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; } = 0.00m;

    public virtual Member? Member { get; set; }
    public virtual ICollection<InvoiceHistoryItem> InvoiceHistoryItems { get; set; } = new List<InvoiceHistoryItem>();
}
