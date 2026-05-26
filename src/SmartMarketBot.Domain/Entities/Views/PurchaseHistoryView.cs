namespace SmartMarketBot.Domain.Entities.Views;

public class PurchaseHistoryView
{
    public int PurchaseID { get; set; }
    public int MemberID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
}
