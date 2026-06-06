using SmartMarketBot.Domain.Common;

namespace SmartMarketBot.Domain.Entities;

public class ShoppingHistory
{
    public int ShoppingHistoryID { get; set; }
    public int MemberID { get; set; }
    public DateTime ShoppingDate { get; set; } = VnDateTime.Now;
    public decimal TotalAmount { get; set; } = 0.00m;
    public string? PaymentMethod { get; set; }

    public virtual Member Member { get; set; } = null!;
    public virtual ICollection<HistoryItem> HistoryItems { get; set; } = new List<HistoryItem>();
}
