namespace SmartMarketBot.Domain.Entities.Views;

public class RealTimeStockView
{
    public int StockID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public string? SubstituteProduct { get; set; }
}
